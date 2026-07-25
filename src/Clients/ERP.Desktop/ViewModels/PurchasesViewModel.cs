using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ERP.Desktop.Services;
using ERP.Shared.Contracts.Products;
using ERP.Shared.Contracts.Purchases;
using ERP.Shared.Contracts.Suppliers;

namespace ERP.Desktop.ViewModels;

/// <summary>Alış yaradarkən müvəqqəti sətir (UI-da göstərilir).</summary>
public sealed record DraftPurchaseLine(Guid ProductId, string ProductName, int Quantity, decimal UnitCost)
{
    public decimal LineTotal => Quantity * UnitCost;
}

/// <summary>Alışlar ekranı — siyahı, yeni alış yaratma, təsdiq/qəbul/ləğv.</summary>
public partial class PurchasesViewModel(ErpApiClient api) : ViewModelBase
{
    public ObservableCollection<PurchaseDto> Purchases { get; } = [];

    [ObservableProperty] private string? _search;

    /// <summary>Canlı axtarış — yazıldıqca süzülür (Enter da işləyir).</summary>
    partial void OnSearchChanged(string? value) => DebounceReload(LoadAsync);
    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private string? _status;
    [ObservableProperty] private PurchaseDto? _selected;

    // --- Yeni alış forması ---
    [ObservableProperty] private bool _showNewPurchase;
    public ObservableCollection<SupplierDto> AllSuppliers { get; } = [];
    public ObservableCollection<ProductDto> AllProducts { get; } = [];
    public ObservableCollection<DraftPurchaseLine> DraftLines { get; } = [];

    [ObservableProperty] private SupplierDto? _newSupplier;
    [ObservableProperty] private DateTimeOffset _newOrderDate = DateTimeOffset.Now;
    [ObservableProperty] private ProductDto? _lineProduct;
    [ObservableProperty] private int _lineQuantity = 1;
    [ObservableProperty] private decimal _lineUnitCost;

    public decimal DraftTotal => DraftLines.Sum(l => l.LineTotal);

    [RelayCommand]
    private async Task ToggleNewPurchaseAsync()
    {
        ShowNewPurchase = !ShowNewPurchase;
        if (ShowNewPurchase && AllSuppliers.Count == 0)
        {
            var sups = await api.GetSuppliersAsync(null);
            if (sups is not null) foreach (var s in sups.Items) AllSuppliers.Add(s);
            var prods = await api.GetProductsAsync(null);
            if (prods is not null) foreach (var p in prods.Items) AllProducts.Add(p);
        }
    }

    [RelayCommand]
    private void AddLine()
    {
        if (LineProduct is null || LineQuantity <= 0) { Status = "Məhsul və say seçin."; return; }
        if (LineUnitCost < 0) { Status = "Alış qiyməti mənfi ola bilməz."; return; }
        if (DraftLines.Any(l => l.ProductId == LineProduct.Id)) { Status = "Bu məhsul artıq əlavə olunub."; return; }

        DraftLines.Add(new DraftPurchaseLine(LineProduct.Id, LineProduct.Name, LineQuantity, LineUnitCost));
        OnPropertyChanged(nameof(DraftTotal));
        LineQuantity = 1;
        LineUnitCost = 0;
    }

    [RelayCommand]
    private void RemoveLine(DraftPurchaseLine line)
    {
        DraftLines.Remove(line);
        OnPropertyChanged(nameof(DraftTotal));
    }

    [RelayCommand]
    private async Task CreatePurchaseAsync()
    {
        if (NewSupplier is null) { Status = "Təchizatçı seçin."; return; }
        if (DraftLines.Count == 0) { Status = "Ən azı bir sətir əlavə edin."; return; }

        var request = new CreatePurchaseRequest(
            SupplierId: NewSupplier.Id,
            OrderDate: DateOnly.FromDateTime(NewOrderDate.DateTime),
            Lines: DraftLines.Select(l => new CreatePurchaseLineRequest(l.ProductId, l.Quantity, l.UnitCost)).ToList());

        var (ok, error) = await api.CreatePurchaseAsync(request);
        if (ok)
        {
            Status = "Alış yaradıldı (Qaralama).";
            DraftLines.Clear();
            NewSupplier = null;
            ShowNewPurchase = false;
            OnPropertyChanged(nameof(DraftTotal));
            await LoadAsync();
        }
        else Status = error ?? "Alış yaradılmadı.";
    }

    [RelayCommand]
    private async Task LoadAsync()
    {
        IsBusy = true;
        Status = "Yüklənir...";
        try
        {
            var result = await api.GetPurchasesAsync(Search);
            Purchases.Clear();
            if (result is not null)
                foreach (var p in result.Items) Purchases.Add(p);
            Status = $"{Purchases.Count} alış";
        }
        catch (System.Exception ex)
        {
            Status = $"Xəta: {ex.Message}";
        }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private async Task ConfirmAsync()
    {
        if (Selected is null) { Status = "Alış seçin."; return; }
        var (ok, error) = await api.ConfirmPurchaseAsync(Selected.Id);
        Status = ok ? "Alış təsdiqləndi." : error ?? "Təsdiqlənmədi.";
        await LoadAsync();
    }

    [RelayCommand]
    private async Task ReceiveAsync()
    {
        if (Selected is null) { Status = "Alış seçin."; return; }
        var (ok, error) = await api.ReceivePurchaseAsync(Selected.Id);
        Status = ok ? "Mal qəbul edildi — anbar stoku artırıldı." : error ?? "Qəbul edilmədi.";
        await LoadAsync();
    }

    [RelayCommand]
    private async Task CancelAsync()
    {
        if (Selected is null) { Status = "Alış seçin."; return; }
        var (ok, error) = await api.CancelPurchaseAsync(Selected.Id);
        Status = ok ? "Alış ləğv edildi." : error ?? "Ləğv edilmədi.";
        await LoadAsync();
    }
}
