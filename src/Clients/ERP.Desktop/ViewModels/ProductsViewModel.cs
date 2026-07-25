using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ERP.Desktop.Converters;
using ERP.Desktop.Services;
using ERP.Shared.Contracts.Products;
using ERP.Shared.Contracts.Warehouses;

namespace ERP.Desktop.ViewModels;

/// <summary>Məhsullar ekranı — icarə avadanlığı kataloqu, siyahı, canlı axtarış, əlavə/redaktə və şəkil (TDD §24).</summary>
public partial class ProductsViewModel : ViewModelBase
{
    private readonly ErpApiClient _api;

    /// <summary>Alış/satış (həssas) qiymətləri görmək icazəsi — yalnız Admin/Menecer (products.viewcost).</summary>
    public bool CanViewCost { get; }

    public ProductsViewModel(ErpApiClient api, bool canViewCost = false)
    {
        _api = api;
        CanViewCost = canViewCost;
    }

    public ObservableCollection<ProductDto> Products { get; } = [];

    [ObservableProperty] private string? _search;
    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private string? _status;
    [ObservableProperty] private ProductDto? _selected;

    /// <summary>İzləmə rejimi seçimləri — istifadəçi üçün aydın adlar.</summary>
    public string[] TrackingModes { get; } =
        [TrackingModeConverter.BulkDisplay, TrackingModeConverter.IndividualDisplay];

    // ---- Canlı axtarış (yazıldıqca, ortaq debounce ilə) ----
    partial void OnSearchChanged(string? value) => DebounceReload(LoadAsync);

    // ---- Şəkil ----
    [ObservableProperty] private Bitmap? _selectedImage;

    partial void OnSelectedChanged(ProductDto? value) => _ = LoadSelectedImageAsync(value);

    private async Task LoadSelectedImageAsync(ProductDto? product)
    {
        SelectedImage = null;
        if (product is null || !product.HasImage) return;
        try
        {
            var bytes = await _api.GetProductImageBytesAsync(product.Id);
            if (bytes is not null)
            {
                using var ms = new MemoryStream(bytes);
                SelectedImage = new Bitmap(ms);
            }
        }
        catch { /* şəkil yüklənə bilmədi — sükutla keç */ }
    }

    /// <summary>Kodun arxasından (fayl seçicidən) çağırılır: şəkli yükləyir və göstərir.</summary>
    public async Task UploadImageAsync(string filePath)
    {
        if (Selected is null) { Status = "Əvvəlcə məhsul seçin."; return; }
        IsBusy = true;
        try
        {
            var (ok, error) = await _api.UploadProductImageAsync(Selected.Id, filePath);
            if (!ok) { Status = error ?? "Şəkil yüklənmədi."; return; }

            // Şəkli fayldan indi göstər — LoadAsync() siyahını təmizləyir və Selected-i sıfırlayır.
            try
            {
                await using var fs = File.OpenRead(filePath);
                SelectedImage = new Bitmap(fs);
            }
            catch { /* şəkil göstərilə bilmədi — yükləmə yenə uğurludur */ }

            Status = "Şəkil yükləndi (bütün istifadəçilər görəcək).";
            await LoadAsync();
        }
        catch (Exception ex)
        {
            Status = $"Şəkil yüklənmədi: {ex.Message}";
        }
        finally { IsBusy = false; }
    }

    // ---- Yeni məhsul forması (SKU avtomatik — istifadəçi yazmır) ----
    [ObservableProperty] private string? _newName;
    [ObservableProperty] private decimal _newRentalPrice;
    [ObservableProperty] private decimal? _newPurchasePrice;
    [ObservableProperty] private decimal? _newSalePrice;
    [ObservableProperty] private string _newTrackingMode = TrackingModeConverter.BulkDisplay;
    [ObservableProperty] private int _newStock;
    [ObservableProperty] private int _newMinStock;
    [ObservableProperty] private string? _newCategory;

    /// <summary>Məhsulun ilkin stokunun yazılacağı anbar (seçim — məcburi deyil).</summary>
    public ObservableCollection<WarehouseDto> Warehouses { get; } = [];
    [ObservableProperty] private WarehouseDto? _newWarehouse;

    // ---- Redaktə forması ----
    [ObservableProperty] private bool _isEditing;
    [ObservableProperty] private string? _editName;
    [ObservableProperty] private decimal _editRentalPrice;
    [ObservableProperty] private decimal? _editPurchasePrice;
    [ObservableProperty] private decimal? _editSalePrice;
    [ObservableProperty] private string _editTrackingMode = TrackingModeConverter.BulkDisplay;
    [ObservableProperty] private int _editStock;
    [ObservableProperty] private int _editMinStock;
    [ObservableProperty] private string? _editCategory;
    [ObservableProperty] private bool _editIsActive = true;

    private static string ToTrackingValue(string? display) =>
        display == TrackingModeConverter.IndividualDisplay ? "Nüsxə" : "Toplu";

    private static string ToTrackingDisplay(string? value) =>
        value == "Nüsxə" ? TrackingModeConverter.IndividualDisplay : TrackingModeConverter.BulkDisplay;

    [RelayCommand]
    private async Task LoadAsync()
    {
        IsBusy = true;
        Status = "Yüklənir...";
        try
        {
            var result = await _api.GetProductsAsync(Search);
            Products.Clear();
            if (result is not null)
                foreach (var p in result.Items) Products.Add(p);
            Status = $"{Products.Count} məhsul";

            // Anbar siyahısını bir dəfə yüklə (yeni məhsul formasında seçim üçün).
            if (Warehouses.Count == 0)
            {
                var whs = await _api.GetWarehousesAsync(null);
                if (whs is not null)
                    foreach (var w in whs.Items) Warehouses.Add(w);
            }
        }
        catch (Exception ex)
        {
            Status = $"Xəta: {ex.Message}";
        }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private async Task AddAsync()
    {
        if (string.IsNullOrWhiteSpace(NewName))
        {
            Status = "Məhsulun adı tələb olunur.";
            return;
        }

        IsBusy = true;
        try
        {
            var (ok, error) = await _api.CreateProductAsync(new CreateProductRequest(
                Name: NewName!,
                RentalPrice: NewRentalPrice,
                TrackingMode: ToTrackingValue(NewTrackingMode),
                Sku: null, // server avtomatik PRD-000001 generasiya edir
                PurchasePrice: CanViewCost ? NewPurchasePrice : null,
                SalePrice: CanViewCost ? NewSalePrice : null,
                StockQuantity: NewStock,
                MinStockQuantity: NewMinStock,
                Category: NewCategory,
                WarehouseId: NewWarehouse?.Id));

            if (ok)
            {
                Status = NewWarehouse is not null
                    ? $"Məhsul əlavə olundu — ilkin stok '{NewWarehouse.Name}' anbarına yazıldı."
                    : "Məhsul əlavə olundu (SKU avtomatik təyin edildi).";
                NewName = NewCategory = null;
                NewRentalPrice = 0; NewStock = 0; NewMinStock = 0;
                NewPurchasePrice = NewSalePrice = null;
                NewTrackingMode = TrackingModeConverter.BulkDisplay;
                NewWarehouse = null;
                await LoadAsync();
            }
            else Status = error ?? "Əlavə edilmədi.";
        }
        finally { IsBusy = false; }
    }

    /// <summary>Seçilmiş məhsulun məlumatlarını redaktə formasına yükləyir.</summary>
    [RelayCommand]
    private void BeginEdit()
    {
        if (Selected is null) { Status = "Redaktə üçün məhsul seçin."; return; }
        EditName = Selected.Name;
        EditRentalPrice = Selected.RentalPrice;
        EditPurchasePrice = Selected.PurchasePrice;
        EditSalePrice = Selected.SalePrice;
        EditTrackingMode = ToTrackingDisplay(Selected.TrackingMode);
        EditStock = Selected.StockQuantity;
        EditMinStock = Selected.MinStockQuantity;
        EditCategory = Selected.Category;
        EditIsActive = Selected.IsActive;
        IsEditing = true;
        Status = $"Redaktə: {Selected.Sku} — {Selected.Name}";
    }

    [RelayCommand]
    private void CancelEdit() => IsEditing = false;

    [RelayCommand]
    private async Task SaveEditAsync()
    {
        if (Selected is null) { IsEditing = false; return; }
        if (string.IsNullOrWhiteSpace(EditName))
        {
            Status = "Məhsulun adı tələb olunur.";
            return;
        }

        IsBusy = true;
        try
        {
            var (ok, error) = await _api.UpdateProductAsync(Selected.Id, new UpdateProductRequest(
                Name: EditName!,
                RentalPrice: EditRentalPrice,
                TrackingMode: ToTrackingValue(EditTrackingMode),
                PurchasePrice: CanViewCost ? EditPurchasePrice : Selected.PurchasePrice,
                SalePrice: CanViewCost ? EditSalePrice : Selected.SalePrice,
                StockQuantity: EditStock,
                MinStockQuantity: EditMinStock,
                Category: EditCategory,
                IsActive: EditIsActive));

            if (ok)
            {
                Status = "Məhsul yeniləndi.";
                IsEditing = false;
                await LoadAsync();
            }
            else Status = error ?? "Yenilənmədi.";
        }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private async Task ExportExcelAsync()
    {
        var bytes = await _api.ExportProductsExcelAsync();
        if (bytes is null) { Status = "İxrac alınmadı."; return; }

        var path = Path.Combine(Path.GetTempPath(), $"mehsullar-{DateTime.Now:yyyyMMdd-HHmmss}.xlsx");
        await File.WriteAllBytesAsync(path, bytes);
        Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
        Status = $"Excel ixrac olundu: {Path.GetFileName(path)}";
    }

    [RelayCommand]
    private async Task ShowQrAsync()
    {
        if (Selected is null) { Status = "Məhsul seçin."; return; }

        var bytes = await _api.GetProductQrAsync(Selected.Id);
        if (bytes is null) { Status = "QR alınmadı."; return; }

        var path = Path.Combine(Path.GetTempPath(), $"qr-{Selected.Sku}.png");
        await File.WriteAllBytesAsync(path, bytes);
        Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
        Status = $"QR kod açıldı: {Selected.Sku}";
    }
}
