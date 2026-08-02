using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ERP.Desktop.Services;
using ERP.Shared.Contracts.Products;
using ERP.Shared.Contracts.Warehouses;
using Microsoft.AspNetCore.SignalR.Client;

namespace ERP.Desktop.ViewModels;

/// <summary>
/// Stok ekranı — per-anbar səviyyələr, adjust, transfer, min-stok filtri. SignalR ilə canlı
/// yenilənir: başqa istifadəçi stoku dəyişəndə siyahı dərhal təzələnir (TDD §38).
/// </summary>
public partial class StockViewModel(ErpApiClient api) : ViewModelBase
{
    private HubConnection? _hub;

    [ObservableProperty] private string _liveStatus = "🔴 Oflayn";

    public ObservableCollection<StockLevelDto> Levels { get; } = [];
    [ObservableProperty] private StockLevelDto? _selectedLevel;
    // #3 — sətir seçiləndə redaktə sahələrini avtomatik doldur (yerindəcə dəyişmək üçün).
    partial void OnSelectedLevelChanged(StockLevelDto? value) { if (value is not null) FillAdjustFromSelected(); }

    /// <summary>Seçilmiş stok sətrinin məhsul tarixçəsi VM-i (kod-arxasından çağırılır).</summary>
    public ProductHistoryViewModel? CreateHistory() =>
        SelectedLevel is null ? null : new ProductHistoryViewModel(api, SelectedLevel.ProductId, $"Tarixçə — {SelectedLevel.ProductName}");
    public ObservableCollection<ProductDto> AllProducts { get; } = [];
    public ObservableCollection<WarehouseDto> AllWarehouses { get; } = [];

    [ObservableProperty] private string? _search;

    /// <summary>Canlı axtarış — yazıldıqca süzülür (Enter da işləyir).</summary>
    partial void OnSearchChanged(string? value) => DebounceReload(LoadAsync);
    [ObservableProperty] private bool _lowOnly;
    // #3 — anbar filtri (yuxarıda) + transfer paneli
    [ObservableProperty] private WarehouseDto? _warehouseFilter;
    partial void OnWarehouseFilterChanged(WarehouseDto? value) => _ = LoadAsync();
    [ObservableProperty] private bool _showTransfer;
    partial void OnLowOnlyChanged(bool value) => _ = LoadAsync();

    /// <summary>#16 — stok redaktə paneli yalnız düymə ilə açılır.</summary>
    [ObservableProperty] private bool _showAdjust;

    [RelayCommand]
    private void ToggleTransfer() { ShowTransfer = !ShowTransfer; if (ShowTransfer) ShowAdjust = false; }

    /// <summary>Seçilmiş sətri redaktə panelinə yükləyib açır (#16 — düymə ilə).</summary>
    [RelayCommand]
    private void EditSelected()
    {
        if (SelectedLevel is null) { ERP.Desktop.AppNotify.Show("Redaktə üçün cədvəldən stok seçin."); return; }
        FillAdjustFromSelected();
        ShowTransfer = false;
        ShowAdjust = true;
    }

    /// <summary>Boş redaktə paneli — yeni məhsul-anbar stoku təyin etmək üçün.</summary>
    [RelayCommand]
    private void NewAdjust()
    {
        AdjProduct = null; AdjWarehouse = WarehouseFilter; AdjQuantity = 0; AdjMinQuantity = 0; AdjInRepair = 0; AdjDamaged = 0;
        ShowTransfer = false;
        ShowAdjust = true;
    }

    /// <summary>#3 — cədvəldə sətrə 2 klik: adjust formasını doldur (yerindəcə redaktə).</summary>
    public void FillAdjustFromSelected()
    {
        if (SelectedLevel is null) return;
        AdjProduct = AllProducts.FirstOrDefault(p => p.Id == SelectedLevel.ProductId);
        AdjWarehouse = AllWarehouses.FirstOrDefault(w => w.Id == SelectedLevel.WarehouseId);
        AdjQuantity = SelectedLevel.Quantity;
        AdjMinQuantity = SelectedLevel.MinQuantity;
        AdjInRepair = SelectedLevel.InRepair;
        AdjDamaged = SelectedLevel.Damaged;
        Status = $"Redaktə: {SelectedLevel.ProductName} @ {SelectedLevel.WarehouseName} — sayı dəyişib 'Təyin et' basın.";
    }
    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private string? _status;
    /// <summary>Dropdown siyahılarını yenidən yükləmək lazımdırmı (yalnız açılışda/Yenilə-də).</summary>
    private bool _reloadLists = true;

    /// <summary>Anbar/məhsul siyahılarını + səviyyələri tam təzələyir (Yenilə düyməsi).</summary>
    [RelayCommand]
    private async Task RefreshAsync() { _reloadLists = true; await LoadAsync(); }

    // Stok təyini (adjust) forması
    [ObservableProperty] private ProductDto? _adjProduct;
    [ObservableProperty] private WarehouseDto? _adjWarehouse;
    [ObservableProperty] private int _adjQuantity;
    [ObservableProperty] private int _adjMinQuantity;
    [ObservableProperty] private int _adjInRepair;
    [ObservableProperty] private int _adjDamaged;

    // Transfer forması
    [ObservableProperty] private ProductDto? _trProduct;
    [ObservableProperty] private WarehouseDto? _trFrom;
    [ObservableProperty] private WarehouseDto? _trTo;
    [ObservableProperty] private int _trQuantity;

    /// <summary>SignalR hub-a qoşulur (bir dəfə) və "StockChanged" hadisəsində siyahını təzələyir.</summary>
    private async Task EnsureLiveConnectionAsync()
    {
        if (_hub is not null) return;

        _hub = new HubConnectionBuilder()
            .WithUrl($"{api.BaseUrl}/hubs/stock")
            .WithAutomaticReconnect()
            .Build();

        _hub.On<StockChangedNotification>("StockChanged", n =>
            Dispatcher.UIThread.Post(() =>
            {
                LiveStatus = $"🟢 Canlı — son: {n.ProductName} @ {n.WarehouseName} = {n.Quantity}"
                    + (n.IsLow ? " ⚠️" : "");
                LoadCommand.Execute(null);
            }));

        _hub.Reconnected += _ => { Dispatcher.UIThread.Post(() => LiveStatus = "🟢 Canlı"); return Task.CompletedTask; };
        _hub.Closed += _ => { Dispatcher.UIThread.Post(() => LiveStatus = "🔴 Oflayn"); return Task.CompletedTask; };

        try
        {
            await _hub.StartAsync();
            LiveStatus = "🟢 Canlı";
        }
        catch (System.Exception ex)
        {
            LiveStatus = $"🔴 Canlı bağlantı yoxdur: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task LoadAsync()
    {
        await EnsureLiveConnectionAsync();
        IsBusy = true;
        Status = "Yüklənir...";
        try
        {
            // Anbar/məhsul dropdown-larını yüklə. Filtr dəyişəndə təkrar TƏMİZLƏMİRİK ki,
            // yuxarıdakı anbar seçimi sıfırlanmasın (#16 — "anbar siyahısı gəlmir" bug).
            if (AllProducts.Count == 0 || _reloadLists)
            {
                var prods = await api.GetProductsAsync(null);
                AllProducts.Clear();
                if (prods is not null) foreach (var p in prods.Items) AllProducts.Add(p);
            }
            if (AllWarehouses.Count == 0 || _reloadLists)
            {
                var whs = await api.GetWarehousesAsync(null);
                AllWarehouses.Clear();
                if (whs is not null) foreach (var w in whs.Items) AllWarehouses.Add(w);
            }
            _reloadLists = false;

            var result = await api.GetStockLevelsAsync(Search, LowOnly);
            Levels.Clear();
            if (result is not null)
                foreach (var l in result.Items)
                    if (WarehouseFilter is null || l.WarehouseId == WarehouseFilter.Id)
                        Levels.Add(l);
            Status = $"{Levels.Count} səviyyə" + (LowOnly ? " (yalnız aşağı)" : "")
                     + (WarehouseFilter is not null ? $" — {WarehouseFilter.Name}" : "");
        }
        catch (System.Exception ex)
        {
            Status = $"Xəta: {ex.Message}";
        }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private async Task AdjustAsync()
    {
        if (AdjProduct is null || AdjWarehouse is null) { Status = "Məhsul və anbar seçin."; return; }

        var (ok, error) = await api.AdjustStockAsync(new AdjustStockRequest(
            ProductId: AdjProduct.Id, WarehouseId: AdjWarehouse.Id,
            Quantity: AdjQuantity, MinQuantity: AdjMinQuantity,
            InRepair: AdjInRepair, Damaged: AdjDamaged));
        Status = ok ? "Stok təyin edildi." : error ?? "Alınmadı.";
        ERP.Desktop.AppNotify.Show(ok ? "✓ Stok təyin edildi." : "⚠ " + (error ?? "Alınmadı."));
        if (ok) { ShowAdjust = false; await LoadAsync(); }
    }

    [RelayCommand]
    private async Task TransferAsync()
    {
        if (TrProduct is null || TrFrom is null || TrTo is null) { Status = "Məhsul və anbarları seçin."; return; }
        if (TrQuantity <= 0) { Status = "Miqdar 0-dan böyük olmalıdır."; return; }

        var (ok, error) = await api.TransferStockAsync(new TransferStockRequest(
            ProductId: TrProduct.Id, FromWarehouseId: TrFrom.Id, ToWarehouseId: TrTo.Id, Quantity: TrQuantity));
        Status = ok ? "Transfer tamamlandı." : error ?? "Transfer alınmadı.";
        ERP.Desktop.AppNotify.Show(ok ? "✓ Transfer tamamlandı." : "⚠ " + (error ?? "Transfer alınmadı."));
        if (ok) { ShowTransfer = false; TrQuantity = 0; await LoadAsync(); }
    }
}
