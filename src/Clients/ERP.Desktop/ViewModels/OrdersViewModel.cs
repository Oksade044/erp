using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ERP.Desktop.Services;
using ERP.Shared.Contracts.Customers;
using ERP.Shared.Contracts.Hr;
using ERP.Shared.Contracts.Orders;
using ERP.Shared.Contracts.Products;

namespace ERP.Desktop.ViewModels;

/// <summary>Sifariş yaradarkən müvəqqəti sətir (UI-da göstərilir).</summary>
public sealed record DraftLine(Guid ProductId, string ProductName, int Quantity, decimal UnitPrice,
    Guid? WarehouseId = null, string? WarehouseName = null)
{
    public decimal LineTotal => Quantity * UnitPrice;
}

/// <summary>Sifarişlər ekranı — siyahı, təsdiq/ləğv/təhvil, faktura, və yeni sifariş yaratma.</summary>
public partial class OrdersViewModel : ViewModelBase
{
    private readonly ErpApiClient api;

    /// <summary>Admin/Menecer sifarişi başqa məsul əməkdaşın adına yarada bilər.</summary>
    public bool CanChooseCreator { get; }

    public OrdersViewModel(ErpApiClient api, bool canChooseCreator = false)
    {
        this.api = api;
        CanChooseCreator = canChooseCreator;
    }

    public ObservableCollection<OrderDto> Orders { get; } = [];

    [ObservableProperty] private string? _search;

    /// <summary>Canlı axtarış — yazıldıqca süzülür (Enter da işləyir).</summary>
    partial void OnSearchChanged(string? value) => DebounceReload(LoadAsync);
    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private string? _status;
    [ObservableProperty] private OrderDto? _selected;

    // --- Yeni sifariş forması ---
    [ObservableProperty] private bool _showNewOrder;
    public ObservableCollection<CustomerDto> AllCustomers { get; } = [];
    public ObservableCollection<ProductDto> AllProducts { get; } = [];
    public ObservableCollection<DraftLine> DraftLines { get; } = [];

    /// <summary>Məsul əməkdaş seçimi üçün (yalnız Admin/Menecer). Boş = özüm.</summary>
    public ObservableCollection<EmployeeDto> AllEmployees { get; } = [];
    [ObservableProperty] private EmployeeDto? _selectedCreator;

    [ObservableProperty] private CustomerDto? _newCustomer;
    [ObservableProperty] private DateTimeOffset _newStartDate = DateTimeOffset.Now;
    [ObservableProperty] private DateTimeOffset _newEndDate = DateTimeOffset.Now.AddDays(1);
    [ObservableProperty] private ProductDto? _lineProduct;
    [ObservableProperty] private int _lineQuantity = 1;

    // #18/#19 — seçilmiş məhsulun anbarlar üzrə mövcudluğu + götürüləcək anbar.
    public ObservableCollection<ProductAvailabilityDto> LineAvailability { get; } = [];
    [ObservableProperty] private ProductAvailabilityDto? _lineWarehouse;

    partial void OnLineProductChanged(ProductDto? value) => _ = LoadAvailabilityAsync(value);

    private async Task LoadAvailabilityAsync(ProductDto? product)
    {
        LineAvailability.Clear();
        LineWarehouse = null;
        if (product is null) return;
        var avail = await api.GetProductAvailabilityAsync(product.Id);
        if (avail is not null)
            foreach (var a in avail) LineAvailability.Add(a);
        // Ən çox boş olan anbarı default seç.
        LineWarehouse = LineAvailability.OrderByDescending(a => a.Free).FirstOrDefault();
    }

    // --- Depozit & qaytarma hesablaşması (seçilmiş sifariş üçün) ---
    [ObservableProperty] private decimal _depositAmount;
    [ObservableProperty] private decimal _damageCharge;
    [ObservableProperty] private decimal _penaltyCharge;
    [ObservableProperty] private string? _settlementNotes;

    public decimal DraftTotal => DraftLines.Sum(l => l.LineTotal);

    [RelayCommand]
    private async Task ToggleNewOrderAsync()
    {
        ShowNewOrder = !ShowNewOrder;
        if (ShowNewOrder && AllCustomers.Count == 0)
        {
            var custs = await api.GetCustomersAsync(null);
            if (custs is not null) foreach (var c in custs.Items) AllCustomers.Add(c);
            var prods = await api.GetProductsAsync(null);
            if (prods is not null) foreach (var p in prods.Items) AllProducts.Add(p);

            // Admin/Menecer üçün məsul əməkdaş siyahısı.
            if (CanChooseCreator && AllEmployees.Count == 0)
            {
                var emps = await api.GetEmployeesAsync(null);
                if (emps is not null) foreach (var e in emps.Items) AllEmployees.Add(e);
            }
        }
    }

    [RelayCommand]
    private void ClearCreator() => SelectedCreator = null;

    /// <summary>Seçilmiş sifariş üçün detal kartı VM-i yaradır (#21) — View kod-arxasından çağırılır.</summary>
    public OrderDetailViewModel? CreateDetail() =>
        Selected is null ? null : new OrderDetailViewModel(api, Selected);

    [RelayCommand]
    private void AddLine()
    {
        if (LineProduct is null || LineQuantity <= 0) { Status = "Məhsul və say seçin."; return; }
        if (DraftLines.Any(l => l.ProductId == LineProduct.Id)) { Status = "Bu məhsul artıq əlavə olunub."; return; }

        // #18/#19 — seçilmiş anbarda kifayət qədər boş varmı?
        if (LineWarehouse is not null && LineQuantity > LineWarehouse.Free)
        {
            Status = $"'{LineWarehouse.WarehouseName}' anbarında boş yalnız {LineWarehouse.Free} ədəddir.";
            return;
        }

        DraftLines.Add(new DraftLine(LineProduct.Id, LineProduct.Name, LineQuantity, LineProduct.RentalPrice,
            LineWarehouse?.WarehouseId, LineWarehouse?.WarehouseName));
        OnPropertyChanged(nameof(DraftTotal));
        LineQuantity = 1;
    }

    [RelayCommand]
    private void RemoveLine(DraftLine line)
    {
        DraftLines.Remove(line);
        OnPropertyChanged(nameof(DraftTotal));
    }

    [RelayCommand]
    private async Task CreateOrderAsync()
    {
        if (NewCustomer is null) { Status = "Müştəri seçin."; return; }
        if (DraftLines.Count == 0) { Status = "Ən azı bir sətir əlavə edin."; return; }

        var request = new CreateOrderRequest(
            CustomerId: NewCustomer.Id,
            StartDate: DateOnly.FromDateTime(NewStartDate.DateTime),
            EndDate: DateOnly.FromDateTime(NewEndDate.DateTime),
            Lines: DraftLines.Select(l => new CreateOrderLineRequest(l.ProductId, l.Quantity, l.UnitPrice, l.WarehouseId)).ToList(),
            // Admin/Menecer məsul əməkdaş seçibsə, sifariş onun adına yazılır (yoxsa özünün).
            CreatedByName: CanChooseCreator ? SelectedCreator?.FullName : null,
            CreatedByRole: CanChooseCreator ? SelectedCreator?.Position : null);

        var (ok, error) = await api.CreateOrderAsync(request);
        if (ok)
        {
            Status = SelectedCreator is not null
                ? $"Sifariş yaradıldı (Qaralama) — məsul: {SelectedCreator.FullName}."
                : "Sifariş yaradıldı (Qaralama).";
            DraftLines.Clear();
            NewCustomer = null;
            SelectedCreator = null;
            ShowNewOrder = false;
            OnPropertyChanged(nameof(DraftTotal));
            await LoadAsync();
        }
        else Status = error ?? "Sifariş yaradılmadı.";
    }

    [RelayCommand]
    private async Task LoadAsync()
    {
        IsBusy = true;
        Status = "Yüklənir...";
        try
        {
            var result = await api.GetOrdersAsync(Search);
            Orders.Clear();
            if (result is not null)
                foreach (var o in result.Items) Orders.Add(o);
            Status = $"{Orders.Count} sifariş";
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
        if (Selected is null) { Status = "Sifariş seçin."; return; }
        var (ok, error) = await api.ConfirmOrderAsync(Selected.Id);
        Status = ok ? "Sifariş təsdiqləndi." : error ?? "Təsdiqlənmədi.";
        await LoadAsync();
    }

    [RelayCommand]
    private async Task CancelAsync()
    {
        if (Selected is null) { Status = "Sifariş seçin."; return; }
        var (ok, error) = await api.CancelOrderAsync(Selected.Id);
        Status = ok ? "Sifariş ləğv edildi." : error ?? "Ləğv edilmədi.";
        await LoadAsync();
    }

    [RelayCommand]
    private async Task DeliverAsync()
    {
        if (Selected is null) { Status = "Sifariş seçin."; return; }
        var (ok, error) = await api.DeliverOrderAsync(Selected.Id);
        Status = ok ? "Sifariş təhvil verildi." : error ?? "Təhvil verilmədi.";
        await LoadAsync();
    }

    [RelayCommand]
    private async Task ReturnAsync()
    {
        if (Selected is null) { Status = "Sifariş seçin."; return; }
        var (ok, error) = await api.ReturnOrderAsync(Selected.Id);
        Status = ok ? "Sifariş qaytarıldı." : error ?? "Qaytarılmadı.";
        await LoadAsync();
    }

    [RelayCommand]
    private async Task CreateInvoiceAsync()
    {
        if (Selected is null) { Status = "Sifariş seçin."; return; }
        var (ok, error) = await api.CreateInvoiceAsync(Selected.Id);
        Status = ok ? "Faktura yaradıldı (Fakturalar bölməsinə baxın)." : error ?? "Faktura yaradılmadı.";
    }

    [RelayCommand]
    private async Task SetDepositAsync()
    {
        if (Selected is null) { Status = "Sifariş seçin."; return; }
        var (ok, error) = await api.SetOrderDepositAsync(Selected.Id, DepositAmount);
        Status = ok ? $"Depozit təyin edildi: {DepositAmount:0.00} AZN." : error ?? "Depozit təyin edilmədi.";
        await LoadAsync();
    }

    [RelayCommand]
    private async Task SettleAsync()
    {
        if (Selected is null) { Status = "Sifariş seçin."; return; }
        var (ok, error) = await api.SettleOrderAsync(Selected.Id, DamageCharge, PenaltyCharge, SettlementNotes);
        Status = ok ? "Hesablaşma qeyd edildi — depozit qaytarması hesablandı." : error ?? "Hesablaşma alınmadı.";
        if (ok) { DamageCharge = PenaltyCharge = 0; SettlementNotes = null; }
        await LoadAsync();
    }
}
