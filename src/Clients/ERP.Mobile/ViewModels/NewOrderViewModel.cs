using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ERP.Mobile.Services;
using ERP.Shared.Contracts.Customers;
using ERP.Shared.Contracts.Orders;
using ERP.Shared.Contracts.Products;
using ERP.Shared.Contracts.Warehouses;

namespace ERP.Mobile.ViewModels;

/// <summary>
/// Təmsilçi üçün SADƏ sifariş yaratma: yalnız öz müştərini seç + məhsul əlavə et + yarat.
/// Müştəri əlavə etmək, tarix, növ, qaralama/bron/təhvil və s. YOXDUR — sifariş yaradıldıqda
/// təmsilçinin borcu bağlanmağa başlayır (server RepresentativeEntry).
/// </summary>
public partial class NewOrderViewModel(MobileApiClient api) : ObservableObject
{
    // Müştəri — yalnız təmsilçiyə təyin edilmiş müştərilər (öz müştəriləri).
    private readonly List<CustomerDto> _allMyCustomers = [];
    [ObservableProperty] private string? _customerSearch;
    [ObservableProperty] private CustomerDto? _selectedCustomer;
    public ObservableCollection<CustomerDto> CustomerResults { get; } = [];

    // Məhsul axtarışı
    [ObservableProperty] private string? _productSearch;
    public ObservableCollection<ProductDto> ProductResults { get; } = [];
    [ObservableProperty] private ProductDto? _selectedProduct;
    [ObservableProperty] private int _lineQuantity = 1;
    [ObservableProperty] private decimal _lineUnitPrice;
    public ObservableCollection<StockLevelDto> ProductStock { get; } = [];
    [ObservableProperty] private StockLevelDto? _selectedWarehouse;

    public ObservableCollection<DraftLine> DraftLines { get; } = [];
    public decimal DraftTotal => DraftLines.Sum(l => l.Quantity * l.UnitPrice);

    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private string? _status;

    /// <summary>Səhifə açılanda təmsilçinin müştərilərini yükləyir.</summary>
    [RelayCommand]
    private async Task LoadCustomersAsync()
    {
        var list = await api.GetMyCustomersAsync();
        _allMyCustomers.Clear();
        _allMyCustomers.AddRange(list);
        FilterCustomers();
        Status = _allMyCustomers.Count == 0 ? "Sizə təyin edilmiş müştəri yoxdur." : $"{_allMyCustomers.Count} müştəri";
    }

    partial void OnCustomerSearchChanged(string? value) => FilterCustomers();
    partial void OnProductSearchChanged(string? value) => _ = SearchProductsAsync();

    private void FilterCustomers()
    {
        CustomerResults.Clear();
        var q = (CustomerSearch ?? "").Trim();
        var items = string.IsNullOrEmpty(q)
            ? _allMyCustomers
            : _allMyCustomers.Where(c => (c.Name ?? "").Contains(q, StringComparison.OrdinalIgnoreCase)
                                       || (c.Phone ?? "").Contains(q, StringComparison.OrdinalIgnoreCase));
        foreach (var c in items.Take(50)) CustomerResults.Add(c);
    }

    public void PickCustomer(CustomerDto? c)
    {
        SelectedCustomer = c;
        if (c is not null) { CustomerSearch = c.Name; CustomerResults.Clear(); }
    }

    private async Task SearchProductsAsync()
    {
        if (string.IsNullOrWhiteSpace(ProductSearch)) { ProductResults.Clear(); return; }
        var list = await api.SearchProductsAsync(ProductSearch);
        ProductResults.Clear();
        foreach (var p in list.Take(15)) ProductResults.Add(p);
    }

    public async Task PickProductAsync(ProductDto? p)
    {
        SelectedProduct = p;
        ProductResults.Clear();
        ProductStock.Clear();
        SelectedWarehouse = null;
        if (p is null) return;
        ProductSearch = p.Name;
        LineUnitPrice = p.RentalPrice;
        var stock = await api.GetProductStockAsync(p.Id);
        foreach (var s in stock) ProductStock.Add(s);
        SelectedWarehouse = ProductStock.FirstOrDefault();
    }

    [RelayCommand]
    private void AddLine()
    {
        if (SelectedProduct is null) { Status = "Məhsul seçin."; return; }
        if (LineQuantity <= 0) { Status = "Say 0-dan böyük olmalıdır."; return; }
        if (DraftLines.Any(l => l.ProductId == SelectedProduct.Id)) { Status = "Bu məhsul artıq əlavə olunub."; return; }

        DraftLines.Add(new DraftLine(
            SelectedProduct.Id, SelectedProduct.Name, LineQuantity, LineUnitPrice,
            SelectedWarehouse?.WarehouseId, SelectedWarehouse?.WarehouseName));
        OnPropertyChanged(nameof(DraftTotal));
        SelectedProduct = null; ProductSearch = null; LineQuantity = 1; LineUnitPrice = 0;
        ProductStock.Clear(); SelectedWarehouse = null;
    }

    public void RemoveLine(DraftLine? line)
    {
        if (line is null) return;
        DraftLines.Remove(line);
        OnPropertyChanged(nameof(DraftTotal));
    }

    [RelayCommand]
    private async Task CreateOrderAsync()
    {
        if (SelectedCustomer is null) { Status = "Müştəri seçin."; return; }
        if (DraftLines.Count == 0) { Status = "Ən azı bir məhsul əlavə edin."; return; }

        IsBusy = true;
        try
        {
            // Tarix/növ təmsilçidən soruşulmur — sadə satış sifarişi (borc bağlanması üçün).
            var today = DateOnly.FromDateTime(DateTime.Today);
            var req = new CreateOrderRequest(
                CustomerId: SelectedCustomer.Id,
                StartDate: today,
                EndDate: today,
                Lines: DraftLines.Select(l => new CreateOrderLineRequest(l.ProductId, l.Quantity, l.UnitPrice, l.WarehouseId)).ToList(),
                OrderType: "Satış");
            var (id, err) = await api.CreateOrderAsync(req);
            if (id is null) { Status = err; return; }

            Status = "Sifariş yaradıldı ✓ — borcunuz bağlanır.";
            Reset();
        }
        finally { IsBusy = false; }
    }

    private void Reset()
    {
        SelectedCustomer = null; CustomerSearch = null;
        DraftLines.Clear(); OnPropertyChanged(nameof(DraftTotal));
        FilterCustomers();
    }
}

/// <summary>Sifariş yaratmada müvəqqəti sətir.</summary>
public sealed record DraftLine(Guid ProductId, string ProductName, int Quantity, decimal UnitPrice, Guid? WarehouseId, string? WarehouseName)
{
    public decimal LineTotal => Quantity * UnitPrice;
    public string Display => $"{ProductName} × {Quantity} = {LineTotal:0.00}" + (WarehouseName is null ? "" : $"  ({WarehouseName})");
}
