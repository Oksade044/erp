using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ERP.Desktop.Services;
using ERP.Shared.Contracts.Customers;
using ERP.Shared.Contracts.Hr;
using ERP.Shared.Contracts.Invoices;
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

    /// <summary>"Yaradan" sütununun görünürlüyü — sahə icazəsindən (order.creator).</summary>
    public bool CanViewCreator { get; }

    public OrdersViewModel(ErpApiClient api, bool canChooseCreator = false, bool canViewCreator = true)
    {
        this.api = api;
        CanChooseCreator = canChooseCreator;
        CanViewCreator = canViewCreator;
    }

    public ObservableCollection<OrderDto> Orders { get; } = [];

    [ObservableProperty] private string? _search;

    /// <summary>Canlı axtarış — yazıldıqca süzülür (Enter da işləyir).</summary>
    partial void OnSearchChanged(string? value) => DebounceReload(LoadAsync);

    // #42 — sifariş növünə görə süzgəc (İcarə / Satış / Hamısı).
    public string[] TypeFilters { get; } = ["Hamısı", "İcarə", "Satış"];
    [ObservableProperty] private string _selectedTypeFilter = "Hamısı";
    partial void OnSelectedTypeFilterChanged(string value) => LoadCommand.Execute(null);
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

    // #33 — sifariş növü (İcarə / Satış).
    public string[] OrderTypes { get; } = ["İcarə", "Satış"];
    [ObservableProperty] private string _newOrderType = "İcarə";
    public bool IsRental => NewOrderType != "Satış";
    partial void OnNewOrderTypeChanged(string value) => OnPropertyChanged(nameof(IsRental));

    [ObservableProperty] private CustomerDto? _newCustomer;
    [ObservableProperty] private DateTimeOffset _newStartDate = DateTimeOffset.Now;
    [ObservableProperty] private DateTimeOffset _newEndDate = DateTimeOffset.Now.AddDays(1);
    [ObservableProperty] private ProductDto? _lineProduct;
    [ObservableProperty] private int _lineQuantity = 1;

    // #29/#30 — sifariş üçün (dəyişdirilə bilən) vahid qiymət; default = məhsulun standart kirayə qiyməti.
    [ObservableProperty] private decimal _lineUnitPrice;
    // #28 — seçilmiş məhsulun şəkli (info panelində).
    [ObservableProperty] private Avalonia.Media.Imaging.Bitmap? _selectedLineImage;

    /// <summary>Say × vahid qiymət — canlı hesablanır (#30).</summary>
    public decimal LinePreviewTotal => LineQuantity * LineUnitPrice;
    partial void OnLineQuantityChanged(int value) => OnPropertyChanged(nameof(LinePreviewTotal));
    partial void OnLineUnitPriceChanged(decimal value) => OnPropertyChanged(nameof(LinePreviewTotal));

    // #18/#19 — seçilmiş məhsulun anbarlar üzrə mövcudluğu + götürüləcək anbar.
    public ObservableCollection<ProductAvailabilityDto> LineAvailability { get; } = [];
    [ObservableProperty] private ProductAvailabilityDto? _lineWarehouse;

    partial void OnLineProductChanged(ProductDto? value)
    {
        // #29 — qiymət standart kirayə qiymətindən başlayır (istifadəçi dəyişə bilər).
        LineUnitPrice = value?.RentalPrice ?? 0;
        OnPropertyChanged(nameof(LinePreviewTotal));
        _ = LoadAvailabilityAsync(value);
        _ = LoadLineImageAsync(value);
    }

    private async Task LoadLineImageAsync(ProductDto? product)
    {
        SelectedLineImage = null;
        if (product is null || !product.HasImage) return;
        try
        {
            var bytes = await api.GetProductImageBytesAsync(product.Id);
            if (bytes is not null)
            {
                using var ms = new System.IO.MemoryStream(bytes);
                SelectedLineImage = new Avalonia.Media.Imaging.Bitmap(ms);
            }
        }
        catch { /* şəkil yüklənə bilmədi */ }
    }

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

    // --- #34/#35 Status dəyişəndə ödəniş pəncərəsi ---
    public string[] PaymentMethods { get; } = ["Nağd", "Köçürmə", "Kart"];
    [ObservableProperty] private bool _showPaymentPrompt;
    [ObservableProperty] private string? _paymentPromptTitle;
    [ObservableProperty] private decimal _paymentAmount;
    [ObservableProperty] private string _paymentMethod = "Nağd";
    [ObservableProperty] private string? _paymentNote;
    // #C — qaytarma zamanı zədə/cərimə (fakturaya yansıyır).
    [ObservableProperty] private bool _isReturnPrompt;
    [ObservableProperty] private decimal _promptDamage;
    [ObservableProperty] private decimal _promptPenalty;
    private Guid? _pendingInvoiceId;
    private Guid _pendingOrderId;

    private async Task OpenPaymentPromptAsync(Guid orderId, string orderNumber, string statusLabel, bool isReturn = false)
    {
        var invs = await api.GetInvoicesAsync(orderNumber);
        var inv = invs?.Items.FirstOrDefault(i => i.OrderNumber == orderNumber);
        if (inv is null) return; // faktura yoxdur (məs. hələ təsdiqlənməyib)
        _pendingInvoiceId = inv.Id;
        _pendingOrderId = orderId;
        IsReturnPrompt = isReturn;
        PromptDamage = 0; PromptPenalty = 0;
        PaymentAmount = inv.Balance > 0 ? inv.Balance : 0;
        PaymentMethod = "Nağd";
        PaymentNote = statusLabel;
        PaymentPromptTitle = isReturn
            ? $"Qaytarıldı. Zədə/cərimə varsa qeyd edin (fakturaya əlavə olunur). Qalıq borc: {inv.Balance:0.00} AZN"
            : $"{statusLabel}. Müştəri ödəniş edib? (qalıq borc: {inv.Balance:0.00} AZN)";
        ShowPaymentPrompt = true;
    }

    [RelayCommand]
    private async Task SubmitPaymentAsync()
    {
        if (_pendingInvoiceId is not { } id) { ShowPaymentPrompt = false; return; }

        // #C — qaytarmada zədə/cərimə → hesablaşma (fakturanın yekun borcunu artırır).
        if (IsReturnPrompt && (PromptDamage > 0 || PromptPenalty > 0))
        {
            var (sok, serr) = await api.SettleOrderAsync(_pendingOrderId, PromptDamage, PromptPenalty, PaymentNote);
            if (!sok) { Status = serr ?? "Hesablaşma alınmadı."; return; }
        }

        if (PaymentAmount > 0)
        {
            var (ok, err) = await api.AddInvoicePaymentAsync(id, new AddPaymentRequest(PaymentAmount, PaymentMethod, null, PaymentNote));
            Status = ok ? $"Ödəniş əlavə olundu: {PaymentAmount:0.00} AZN ({PaymentMethod})" : (err ?? "Ödəniş alınmadı.");
        }
        else Status = IsReturnPrompt ? "Hesablaşma qeyd olundu." : "Ödəniş məbləği daxil edilmədi.";

        ShowPaymentPrompt = false;
        await LoadAsync();
    }

    [RelayCommand]
    private void SkipPayment()
    {
        ShowPaymentPrompt = false;
        Status = "Heç bir ödəniş edilmədi.";
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

    /// <summary>Kateqoriya-məhsul seçim pəncərəsi üçün VM (kod-arxasından açılır, #5).</summary>
    public ProductPickerViewModel CreatePicker() => new(api);

    /// <summary>Seçim pəncərəsindən gələn məhsulları sifariş sətirlərinə əlavə edir (say 1).</summary>
    public void AddPickedProducts(System.Collections.Generic.IEnumerable<ERP.Shared.Contracts.Products.ProductDto> picked)
    {
        int added = 0;
        foreach (var p in picked)
        {
            if (DraftLines.Any(l => l.ProductId == p.Id)) continue;
            DraftLines.Add(new DraftLine(p.Id, p.Name, 1, p.RentalPrice));
            added++;
        }
        OnPropertyChanged(nameof(DraftTotal));
        Status = added > 0 ? $"{added} məhsul əlavə olundu." : "Yeni məhsul seçilmədi.";
    }

    [RelayCommand]
    private void AddLine()
    {
        if (LineProduct is null || LineQuantity <= 0) { Status = "Məhsul və say seçin."; return; }
        if (LineUnitPrice < 0) { Status = "Qiymət mənfi ola bilməz."; return; }
        if (DraftLines.Any(l => l.ProductId == LineProduct.Id)) { Status = "Bu məhsul artıq əlavə olunub."; return; }

        // #18/#19 — seçilmiş anbarda kifayət qədər boş varmı?
        if (LineWarehouse is not null && LineQuantity > LineWarehouse.Free)
        {
            Status = $"'{LineWarehouse.WarehouseName}' anbarında boş yalnız {LineWarehouse.Free} ədəddir.";
            return;
        }

        // #29/#30 — istifadəçinin daxil etdiyi (dəyişdirilə bilən) vahid qiymət istifadə olunur.
        DraftLines.Add(new DraftLine(LineProduct.Id, LineProduct.Name, LineQuantity, LineUnitPrice,
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
            CreatedByRole: CanChooseCreator ? SelectedCreator?.Position : null,
            OrderType: NewOrderType);

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
                foreach (var o in result.Items)
                    if (SelectedTypeFilter == "Hamısı" || o.OrderType == SelectedTypeFilter)
                        Orders.Add(o);
            Status = $"{Orders.Count} sifariş" + (SelectedTypeFilter == "Hamısı" ? "" : $" ({SelectedTypeFilter})");
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
        var num = Selected.OrderNumber; var id = Selected.Id;
        var (ok, error) = await api.ConfirmOrderAsync(Selected.Id);
        Status = ok ? "Sifariş təsdiqləndi." : error ?? "Təsdiqlənmədi.";
        await LoadAsync();
        if (ok) await OpenPaymentPromptAsync(id, num, "Təsdiqləndi");
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
        var num = Selected.OrderNumber; var id = Selected.Id;
        var (ok, error) = await api.DeliverOrderAsync(Selected.Id);
        Status = ok ? "Sifariş təhvil verildi." : error ?? "Təhvil verilmədi.";
        await LoadAsync();
        if (ok) await OpenPaymentPromptAsync(id, num, "Təhvil verildi");
    }

    [RelayCommand]
    private async Task ReturnAsync()
    {
        if (Selected is null) { Status = "Sifariş seçin."; return; }
        var num = Selected.OrderNumber; var id = Selected.Id;
        var (ok, error) = await api.ReturnOrderAsync(Selected.Id);
        Status = ok ? "Sifariş qaytarıldı." : error ?? "Qaytarılmadı.";
        await LoadAsync();
        if (ok) await OpenPaymentPromptAsync(id, num, "Qaytarıldı", isReturn: true);
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
