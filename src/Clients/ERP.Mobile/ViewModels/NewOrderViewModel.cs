using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ERP.Mobile.Services;
using ERP.Shared.Contracts.Customers;
using ERP.Shared.Contracts.Orders;
using ERP.Shared.Contracts.Products;
using ERP.Shared.Contracts.Warehouses;

namespace ERP.Mobile.ViewModels;

/// <summary>Yeni sifariş — müştəri (axtar/yarat) + məhsul axtarışı + anbar seçimi + sətirlər.</summary>
public partial class NewOrderViewModel(MobileApiClient api) : ObservableObject
{
    // Müştəri
    [ObservableProperty] private string? _customerSearch;
    [ObservableProperty] private CustomerDto? _selectedCustomer;
    public ObservableCollection<CustomerDto> CustomerResults { get; } = [];

    // Yeni müştəri
    [ObservableProperty] private bool _creatingCustomer;
    [ObservableProperty] private string? _newCustomerName;
    [ObservableProperty] private string? _newCustomerPhone;
    [ObservableProperty] private string? _newCustomerAddress;

    // Tarixlər
    [ObservableProperty] private DateTime _startDate = DateTime.Today;
    [ObservableProperty] private DateTime _endDate = DateTime.Today.AddDays(1);
    [ObservableProperty] private string _orderType = "İcarə";
    public string[] OrderTypes { get; } = ["İcarə", "Satış"];

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

    partial void OnCustomerSearchChanged(string? value) => _ = SearchCustomersAsync();
    partial void OnProductSearchChanged(string? value) => _ = SearchProductsAsync();

    private async Task SearchCustomersAsync()
    {
        if (string.IsNullOrWhiteSpace(CustomerSearch)) { CustomerResults.Clear(); return; }
        var list = await api.SearchCustomersAsync(CustomerSearch);
        CustomerResults.Clear();
        foreach (var c in list.Take(10)) CustomerResults.Add(c);
    }

    private async Task SearchProductsAsync()
    {
        if (string.IsNullOrWhiteSpace(ProductSearch)) { ProductResults.Clear(); return; }
        var list = await api.SearchProductsAsync(ProductSearch);
        ProductResults.Clear();
        foreach (var p in list.Take(10)) ProductResults.Add(p);
    }

    [RelayCommand]
    private void ToggleCreatingCustomer() => CreatingCustomer = !CreatingCustomer;

    [RelayCommand]
    private void PickCustomer(CustomerDto? c)
    {
        SelectedCustomer = c;
        if (c is not null) { CustomerSearch = c.Name; CustomerResults.Clear(); }
    }

    [RelayCommand]
    private async Task CreateCustomerAsync()
    {
        if (string.IsNullOrWhiteSpace(NewCustomerName) || string.IsNullOrWhiteSpace(NewCustomerPhone))
        { Status = "Müştəri adı və telefon tələb olunur."; return; }
        var (id, err) = await api.CreateCustomerAsync(new CreateCustomerRequest(
            Type: "Fərdi", Name: NewCustomerName!, Phone: NewCustomerPhone!, AddressLine: NewCustomerAddress));
        if (id is null) { Status = err; return; }
        SelectedCustomer = new CustomerDto(id.Value, "Fərdi", NewCustomerName!, NewCustomerPhone!, null, null, NewCustomerAddress, null, null, true, DateTimeOffset.Now);
        CreatingCustomer = false;
        Status = "Müştəri yaradıldı və seçildi.";
    }

    [RelayCommand]
    private async Task PickProductAsync(ProductDto? p)
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
        // sıfırla
        SelectedProduct = null; ProductSearch = null; LineQuantity = 1; LineUnitPrice = 0;
        ProductStock.Clear(); SelectedWarehouse = null;
    }

    [RelayCommand]
    private void RemoveLine(DraftLine? line)
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
        if (EndDate < StartDate) { Status = "Bitmə tarixi başlanğıcdan əvvəl ola bilməz."; return; }

        IsBusy = true;
        try
        {
            var req = new CreateOrderRequest(
                CustomerId: SelectedCustomer.Id,
                StartDate: DateOnly.FromDateTime(StartDate),
                EndDate: DateOnly.FromDateTime(EndDate),
                Lines: DraftLines.Select(l => new CreateOrderLineRequest(l.ProductId, l.Quantity, l.UnitPrice, l.WarehouseId)).ToList(),
                OrderType: OrderType);
            var (id, err) = await api.CreateOrderAsync(req);
            if (id is null) { Status = err; return; }

            Status = "Sifariş yaradıldı ✓";
            Reset();
        }
        finally { IsBusy = false; }
    }

    private void Reset()
    {
        SelectedCustomer = null; CustomerSearch = null; CustomerResults.Clear();
        DraftLines.Clear(); OnPropertyChanged(nameof(DraftTotal));
        StartDate = DateTime.Today; EndDate = DateTime.Today.AddDays(1); OrderType = "İcarə";
    }
}

/// <summary>Sifariş yaratmada müvəqqəti sətir.</summary>
public sealed record DraftLine(Guid ProductId, string ProductName, int Quantity, decimal UnitPrice, Guid? WarehouseId, string? WarehouseName)
{
    public decimal LineTotal => Quantity * UnitPrice;
    public string Display => $"{ProductName} × {Quantity} = {LineTotal:0.00}" + (WarehouseName is null ? "" : $"  ({WarehouseName})");
}
