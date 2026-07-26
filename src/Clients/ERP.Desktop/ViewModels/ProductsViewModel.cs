using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
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

    /// <summary>Anbar siyahısı (yeni məhsulun anbar-stoklarını seçmək üçün).</summary>
    public ObservableCollection<WarehouseDto> Warehouses { get; } = [];
    [ObservableProperty] private WarehouseDto? _newWarehouse;

    /// <summary>Yeni məhsulun anbarlar üzrə ilkin stokları (məhsul bir neçə anbarda ola bilər).</summary>
    public ObservableCollection<NewStockRow> NewStocks { get; } = [];
    [ObservableProperty] private WarehouseDto? _addWarehouse;
    [ObservableProperty] private int _addQty;
    [ObservableProperty] private int _addMin;

    [RelayCommand]
    private void AddNewStockRow()
    {
        if (AddWarehouse is null) { Status = "Anbar seçin."; return; }
        if (NewStocks.Any(r => r.WarehouseId == AddWarehouse.Id)) { Status = "Bu anbar artıq siyahıdadır."; return; }
        NewStocks.Add(new NewStockRow(AddWarehouse.Id, AddWarehouse.Name, AddQty, AddMin));
        AddWarehouse = null; AddQty = 0; AddMin = 0;
    }

    [RelayCommand]
    private void RemoveNewStockRow(NewStockRow row) => NewStocks.Remove(row);

    /// <summary>Mövcud kateqoriyalar — məhsul formasında seçim/yeni yazmaq üçün.</summary>
    public ObservableCollection<string> CategoryNames { get; } = [];

    /// <summary>Yeni məhsul üçün seçilmiş şəkil faylı (yaradılan kimi yüklənir). Boş ola bilər.</summary>
    [ObservableProperty] private string? _newImagePath;

    /// <summary>Seçilmiş şəklin adı (formada göstərmək üçün).</summary>
    public string? NewImageName => string.IsNullOrEmpty(NewImagePath) ? null : Path.GetFileName(NewImagePath);
    partial void OnNewImagePathChanged(string? value) => OnPropertyChanged(nameof(NewImageName));

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

    // ---- #17: redaktədə anbarlar üzrə stok ----
    private Guid _editProductId;

    /// <summary>Redaktə olunan məhsulun anbarlar üzrə stoku (hər sətir ayrıca redaktə olunur).</summary>
    public ObservableCollection<ProductStockRow> EditStockLevels { get; } = [];

    /// <summary>Yeni anbara stok əlavə etmək / transfer üçün.</summary>
    [ObservableProperty] private WarehouseDto? _stockWarehouse;
    [ObservableProperty] private int _stockQty;
    [ObservableProperty] private int _stockMin;
    [ObservableProperty] private WarehouseDto? _transferFrom;
    [ObservableProperty] private WarehouseDto? _transferTo;
    [ObservableProperty] private int _transferQty;

    private async Task LoadEditStockAsync()
    {
        EditStockLevels.Clear();
        var levels = await _api.GetProductStockAsync(_editProductId);
        if (levels is not null)
            foreach (var l in levels)
                EditStockLevels.Add(new ProductStockRow(l.WarehouseId, l.WarehouseName, l.Quantity, l.MinQuantity));
    }

    /// <summary>Bir anbardakı stok sətrini yadda saxlayır (mütləq say + min).</summary>
    [RelayCommand]
    private async Task SaveStockRowAsync(ProductStockRow row)
    {
        if (row is null) return;
        var (ok, error) = await _api.AdjustStockAsync(new AdjustStockRequest(
            _editProductId, row.WarehouseId, row.Quantity, row.MinQuantity));
        Status = ok ? $"'{row.WarehouseName}' stoku yeniləndi: {row.Quantity}" : (error ?? "Yenilənmədi.");
        if (ok) await LoadEditStockAsync();
    }

    /// <summary>Məhsulu seçilmiş anbara yerləşdirir (yeni səviyyə və ya mövcudu dəyişir).</summary>
    [RelayCommand]
    private async Task AddStockToWarehouseAsync()
    {
        if (StockWarehouse is null) { Status = "Anbar seçin."; return; }
        var (ok, error) = await _api.AdjustStockAsync(new AdjustStockRequest(
            _editProductId, StockWarehouse.Id, StockQty, StockMin));
        if (ok)
        {
            Status = $"'{StockWarehouse.Name}' anbarına yazıldı: {StockQty}";
            StockWarehouse = null; StockQty = 0; StockMin = 0;
            await LoadEditStockAsync();
        }
        else Status = error ?? "Alınmadı.";
    }

    /// <summary>Məhsulu bir anbardan digərinə köçürür (#17 — başqa anbara köçürmə).</summary>
    [RelayCommand]
    private async Task TransferStockAsync()
    {
        if (TransferFrom is null || TransferTo is null) { Status = "Mənbə və təyinat anbarını seçin."; return; }
        if (TransferQty <= 0) { Status = "Köçürülən say 0-dan böyük olmalıdır."; return; }
        var (ok, error) = await _api.TransferStockAsync(new TransferStockRequest(
            _editProductId, TransferFrom.Id, TransferTo.Id, TransferQty));
        if (ok)
        {
            Status = $"{TransferQty} ədəd '{TransferFrom.Name}' → '{TransferTo.Name}' köçürüldü.";
            TransferQty = 0;
            await LoadEditStockAsync();
        }
        else Status = error ?? "Köçürülmədi.";
    }

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

            // Kateqoriyaları həmişə təzələ (yeni əlavə olunanlar dərhal görünsün).
            var cats = await _api.GetCategoriesAsync();
            CategoryNames.Clear();
            if (cats is not null)
                foreach (var c in cats) CategoryNames.Add(c.Name);
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
            var initialStocks = NewStocks
                .Select(r => new InitialStockRequest(r.WarehouseId, r.Quantity, r.MinQuantity))
                .ToList();

            var (ok, id, error) = await _api.CreateProductAsync(new CreateProductRequest(
                Name: NewName!,
                RentalPrice: NewRentalPrice,
                TrackingMode: ToTrackingValue(NewTrackingMode),
                Sku: null, // server avtomatik PRD-000001 generasiya edir
                PurchasePrice: CanViewCost ? NewPurchasePrice : null,
                SalePrice: CanViewCost ? NewSalePrice : null,
                MinStockQuantity: NewMinStock,
                Category: NewCategory,
                InitialStocks: initialStocks));

            if (ok)
            {
                // Şəkil seçilibsə, yeni məhsula yüklə.
                var imageNote = "";
                if (id is { } newId && !string.IsNullOrEmpty(NewImagePath))
                {
                    var (imgOk, imgErr) = await _api.UploadProductImageAsync(newId, NewImagePath);
                    imageNote = imgOk ? " + şəkil yükləndi" : $" (şəkil yüklənmədi: {imgErr})";
                }

                Status = (initialStocks.Count > 0
                    ? $"Məhsul əlavə olundu — {initialStocks.Count} anbara stok yazıldı."
                    : "Məhsul əlavə olundu (SKU avtomatik təyin edildi).") + imageNote;

                NewName = NewCategory = null;
                NewRentalPrice = 0; NewStock = 0; NewMinStock = 0;
                NewPurchasePrice = NewSalePrice = null;
                NewTrackingMode = TrackingModeConverter.BulkDisplay;
                NewWarehouse = null;
                NewStocks.Clear();
                NewImagePath = null;
                await LoadAsync();
            }
            else Status = error ?? "Əlavə edilmədi.";
        }
        finally { IsBusy = false; }
    }

    /// <summary>Formada yazılmış kateqoriya adını müstəqil kateqoriya kimi yaradır (əvvəlcədən).</summary>
    [RelayCommand]
    private async Task AddCategoryAsync()
    {
        var name = (IsEditing ? EditCategory : NewCategory)?.Trim();
        if (string.IsNullOrWhiteSpace(name)) { Status = "Kateqoriya adını yazın."; return; }

        var (ok, error) = await _api.CreateCategoryAsync(name);
        if (!ok) { Status = error ?? "Kateqoriya yaradılmadı."; return; }

        if (!CategoryNames.Contains(name)) CategoryNames.Add(name);
        Status = $"Kateqoriya hazır: {name}";
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
        _editProductId = Selected.Id;
        IsEditing = true;
        Status = $"Redaktə: {Selected.Sku} — {Selected.Name}";
        _ = LoadEditStockAsync(); // anbarlar üzrə stoku gətir (#17)
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

/// <summary>Yeni məhsul əlavə edilərkən bir anbardakı ilkin stok sətri (çox-anbar).</summary>
public sealed record NewStockRow(Guid WarehouseId, string WarehouseName, int Quantity, int MinQuantity);

/// <summary>Məhsul redaktəsində bir anbardakı stok sətri (redaktə oluna bilər — #17).</summary>
public partial class ProductStockRow(Guid warehouseId, string warehouseName, int quantity, int minQuantity)
    : ObservableObject
{
    public Guid WarehouseId { get; } = warehouseId;
    public string WarehouseName { get; } = warehouseName;

    [ObservableProperty] private int _quantity = quantity;
    [ObservableProperty] private int _minQuantity = minQuantity;
}
