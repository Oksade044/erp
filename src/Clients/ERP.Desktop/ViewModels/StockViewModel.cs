using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ERP.Desktop.Services;
using ERP.Shared.Contracts.Products;
using ERP.Shared.Contracts.Warehouses;

namespace ERP.Desktop.ViewModels;

/// <summary>Stok ekranı — per-anbar səviyyələr, stok təyini (adjust), anbarlar arası transfer, min-stok filtri.</summary>
public partial class StockViewModel(ErpApiClient api) : ViewModelBase
{
    public ObservableCollection<StockLevelDto> Levels { get; } = [];
    public ObservableCollection<ProductDto> AllProducts { get; } = [];
    public ObservableCollection<WarehouseDto> AllWarehouses { get; } = [];

    [ObservableProperty] private string? _search;
    [ObservableProperty] private bool _lowOnly;
    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private string? _status;

    // Stok təyini (adjust) forması
    [ObservableProperty] private ProductDto? _adjProduct;
    [ObservableProperty] private WarehouseDto? _adjWarehouse;
    [ObservableProperty] private int _adjQuantity;
    [ObservableProperty] private int _adjMinQuantity;

    // Transfer forması
    [ObservableProperty] private ProductDto? _trProduct;
    [ObservableProperty] private WarehouseDto? _trFrom;
    [ObservableProperty] private WarehouseDto? _trTo;
    [ObservableProperty] private int _trQuantity;

    [RelayCommand]
    private async Task LoadAsync()
    {
        IsBusy = true;
        Status = "Yüklənir...";
        try
        {
            if (AllProducts.Count == 0)
            {
                var prods = await api.GetProductsAsync(null);
                if (prods is not null) foreach (var p in prods.Items) AllProducts.Add(p);
            }
            if (AllWarehouses.Count == 0)
            {
                var whs = await api.GetWarehousesAsync(null);
                if (whs is not null) foreach (var w in whs.Items) AllWarehouses.Add(w);
            }

            var result = await api.GetStockLevelsAsync(Search, LowOnly);
            Levels.Clear();
            if (result is not null)
                foreach (var l in result.Items) Levels.Add(l);
            Status = $"{Levels.Count} səviyyə" + (LowOnly ? " (yalnız aşağı)" : "");
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
            Quantity: AdjQuantity, MinQuantity: AdjMinQuantity));
        Status = ok ? "Stok təyin edildi." : error ?? "Alınmadı.";
        if (ok) await LoadAsync();
    }

    [RelayCommand]
    private async Task TransferAsync()
    {
        if (TrProduct is null || TrFrom is null || TrTo is null) { Status = "Məhsul və anbarları seçin."; return; }
        if (TrQuantity <= 0) { Status = "Miqdar 0-dan böyük olmalıdır."; return; }

        var (ok, error) = await api.TransferStockAsync(new TransferStockRequest(
            ProductId: TrProduct.Id, FromWarehouseId: TrFrom.Id, ToWarehouseId: TrTo.Id, Quantity: TrQuantity));
        Status = ok ? "Transfer tamamlandı." : error ?? "Transfer alınmadı.";
        if (ok) await LoadAsync();
    }
}
