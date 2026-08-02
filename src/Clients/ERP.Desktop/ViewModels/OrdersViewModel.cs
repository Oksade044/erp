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

/// <summary>Sifariş yaradarkən müvəqqəti sətir (#6 — say/qiymət cədvəldə redaktə olunur).</summary>
public sealed partial class DraftLine : ObservableObject
{
    public Guid ProductId { get; }
    public string ProductName { get; }
    public Guid? WarehouseId { get; }
    public string? WarehouseName { get; }

    /// <summary>#3 — bu anbarda mövcud boş say (say redaktə olunanda xəbərdarlıq üçün). 0 = naməlum.</summary>
    public int MaxAvailable { get; }

    /// <summary>#7 — sətrin valyutası (məhsuldan gəlir). Bir sifarişdə hamısı eyni olmalıdır.</summary>
    public string Currency { get; }

    [ObservableProperty] private int _quantity;
    [ObservableProperty] private decimal _unitPrice;

    public DraftLine(Guid productId, string productName, int quantity, decimal unitPrice,
        Guid? warehouseId = null, string? warehouseName = null, int maxAvailable = 0, string currency = "AZN")
    {
        ProductId = productId;
        ProductName = productName;
        _quantity = quantity;
        _unitPrice = unitPrice;
        WarehouseId = warehouseId;
        WarehouseName = warehouseName;
        MaxAvailable = maxAvailable;
        Currency = currency;
    }

    /// <summary>#3 — say anbardakı boş saydan çoxdursa cədvəldə görünən xəbərdarlıq.</summary>
    public bool ExceedsStock => MaxAvailable > 0 && Quantity > MaxAvailable;
    public string StockHint => MaxAvailable > 0 ? $"boş: {MaxAvailable}" : "";

    public decimal LineTotal => Quantity * UnitPrice;
    partial void OnQuantityChanged(int value)
    {
        OnPropertyChanged(nameof(LineTotal));
        OnPropertyChanged(nameof(ExceedsStock));
        // #3/#4 — say boş saydan çox olarsa ekran ortası xəbərdarlıq.
        if (MaxAvailable > 0 && value > MaxAvailable)
            ERP.Desktop.AppNotify.Show($"⚠ '{ProductName}' — anbarda boş yalnız {MaxAvailable} ədəddir (istənilən: {value}).");
    }
    partial void OnUnitPriceChanged(decimal value) => OnPropertyChanged(nameof(LineTotal));
}

/// <summary>
/// Sifariş siyahısı sətri — statusa görə kontekstual düymələr və checkbox seçimi (#17).
/// </summary>
public partial class OrderRow : ObservableObject
{
    public OrderDto Dto { get; }
    [ObservableProperty] private bool _isSelected;
    public OrderRow(OrderDto dto) => Dto = dto;

    public Guid Id => Dto.Id;
    public string OrderNumber => Dto.OrderNumber;
    public string CustomerName => Dto.CustomerName;
    public string Status => Dto.Status;
    public string OrderType => Dto.OrderType;
    // Sistem administratoru (mərkəzi ofis) → "Mərkəz" kimi göstərilir.
    public string? CreatedByName => Dto.CreatedByName is "Sistem Administratoru" or null or "" ? "Mərkəz" : Dto.CreatedByName;
    public string? CreatedByRole => Dto.CreatedByRole;
    public decimal Total => Dto.Total;
    public string Currency => Dto.Currency;
    public decimal Deposit => Dto.Deposit;
    public string DepositStatus => Dto.DepositStatus;
    public decimal TotalCharges => Dto.TotalCharges;
    public decimal DepositRefund => Dto.DepositRefund;
    public DateOnly StartDate => Dto.StartDate;
    public DateOnly EndDate => Dto.EndDate;

    // Statusa görə kontekstual düymələrin görünürlüyü.
    public bool IsDraft => Status == "Qaralama";
    public bool IsConfirmed => Status == "Təsdiqlənmiş";
    public bool IsDelivered => Status == "TəhvilVerilmiş";
    public bool IsReturned => Status == "Qaytarılmış";
    public bool IsCancelled => Status == "Ləğv";

    /// <summary>Qaralama → düzənlə/təhvil/ləğv.</summary>
    public bool CanEdit => IsDraft;
    public bool CanDeliver => IsDraft || IsConfirmed;
    public bool CanCancel => IsDraft || IsConfirmed;
    public bool CanReturn => IsDelivered;
    /// <summary>Faktura təsdiqdən sonra mövcuddur.</summary>
    public bool HasInvoice => !IsDraft && !IsCancelled;
}

/// <summary>Sifarişlər ekranı — siyahı, təsdiq/ləğv/təhvil, faktura, və yeni sifariş yaratma.</summary>
public partial class OrdersViewModel : ViewModelBase
{
    private readonly ErpApiClient api;

    /// <summary>Admin/Menecer sifarişi başqa məsul əməkdaşın adına yarada bilər.</summary>
    public bool CanChooseCreator { get; }

    /// <summary>"Yaradan" sütununun görünürlüyü — sahə icazəsindən (order.creator).</summary>
    public bool CanViewCreator { get; }

    /// <summary>#3 — bu ekran yalnız sifariş yaratma üçündür (siyahı ayrı bölmədədir).</summary>
    public bool CreateMode { get; }
    public bool ListMode => !CreateMode;

    /// <summary>#B — status qrupu filtri (null=hamısı, "Qaralama", "Təhvil", "Qaytarılanlar", "Ləğv").</summary>
    private readonly string? _statusGroup;

    public OrdersViewModel(ErpApiClient api, bool canChooseCreator = false, bool canViewCreator = true,
        bool createMode = false, string? statusGroup = null)
    {
        this.api = api;
        CanChooseCreator = canChooseCreator;
        CanViewCreator = canViewCreator;
        CreateMode = createMode;
        _statusGroup = statusGroup;
        if (createMode)
        {
            ShowNewOrder = true;
            _ = EnsureFormDataAsync();
        }
    }

    /// <summary>Sifarişin statusu bu bölmənin status qrupuna uyğundurmu.</summary>
    private bool MatchesStatusGroup(string status) => _statusGroup switch
    {
        null => true,
        "Qaralama" => status == "Qaralama",
        "Bron" => status == "Təsdiqlənmiş",         // rezerv edilib, hələ aparılmayıb
        "Aparıldı" => status == "TəhvilVerilmiş",    // müştəri aparıb (əvvəlki "təhvildə")
        "Qaytarılanlar" => status == "Qaytarılmış",
        "Ləğv" => status == "Ləğv",
        _ => true
    };

    public ObservableCollection<OrderRow> Orders { get; } = [];

    [ObservableProperty] private string? _search;

    /// <summary>Canlı axtarış — yazıldıqca süzülür (Enter da işləyir).</summary>
    partial void OnSearchChanged(string? value) => DebounceReload(LoadAsync);

    // #42 — sifariş növünə görə süzgəc (İcarə / Satış / Hamısı).
    public string[] TypeFilters { get; } = ["Hamısı", "İcarə", "Satış"];
    [ObservableProperty] private string _selectedTypeFilter = "Hamısı";
    partial void OnSelectedTypeFilterChanged(string value) => LoadCommand.Execute(null);
    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private string? _status;
    [ObservableProperty] private OrderRow? _selected;
    private OrderDto? SelectedDto => Selected?.Dto;

    // --- Yeni sifariş forması ---
    [ObservableProperty] private bool _showNewOrder;

    /// <summary>#18 — qaralama düzənləmə rejimi (siyahı bölməsində formanı göstərir).</summary>
    [ObservableProperty] private bool _isEditingOrder;
    partial void OnIsEditingOrderChanged(bool value)
    {
        OnPropertyChanged(nameof(FormVisible));
        OnPropertyChanged(nameof(SaveButtonText));
    }
    private Guid? _editingOrderId;
    // Düzənləmə zamanı sifarişin əvvəlki "məsul əməkdaş"ı qorunur (sıfırlanmasın).
    private string? _editCreatedByName;
    private string? _editCreatedByRole;

    /// <summary>Yadda saxla düyməsinin mətni — yaratma və ya düzənləmə.</summary>
    public string SaveButtonText => IsEditingOrder ? "✓ Yadda saxla" : "Sifarişi yarat";

    /// <summary>#18 — düzənləmədən çıx (dəyişiklikləri saxlamadan).</summary>
    [RelayCommand]
    private void CancelEdit()
    {
        _editingOrderId = null;
        _editCreatedByName = null;
        _editCreatedByRole = null;
        IsEditingOrder = false;
        ShowNewOrder = false;
        DraftLines.Clear();
        NewCustomer = null;
        NewDeposit = 0;
        OnPropertyChanged(nameof(DraftTotal));
        ERP.Desktop.AppNotify.Show("Düzənləmədən çıxıldı.");
    }

    /// <summary>Yeni sifariş forması görünürlüyü: yaratma bölməsi VƏ YA düzənləmə.</summary>
    public bool FormVisible => CreateMode || IsEditingOrder;

    /// <summary>#17 — depozit yalnız yaratma/qaralama zamanı (icarə üçün).</summary>
    [ObservableProperty] private decimal _newDeposit;

    /// <summary>#B — yaratma rejimi: "Qaralama" (layihə) və ya "Bron et" (rezerv).</summary>
    public string[] CreateModes { get; } = ["Qaralama", "Bron et"];
    [ObservableProperty] private string _selectedCreateMode = "Qaralama";
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
    partial void OnShowPaymentPromptChanged(bool value) => OnPropertyChanged(nameof(ShowDepositPanel));
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

        // #4 — qeyd mütləqdir.
        if (string.IsNullOrWhiteSpace(PaymentNote))
        {
            ERP.Desktop.AppNotify.Show("⚠ Qeyd mütləqdir — ödəniş/hesablaşma üçün qeyd yazın.");
            return;
        }

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
            ERP.Desktop.AppNotify.Show(ok ? $"✓ Ödəniş: {PaymentAmount:0.00} AZN ({PaymentMethod})" : (err ?? "Ödəniş alınmadı."));
        }
        else { Status = IsReturnPrompt ? "Hesablaşma qeyd olundu." : "Ödəniş məbləği daxil edilmədi."; ERP.Desktop.AppNotify.Show(Status); }

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

    /// <summary>#7 — sifarişin valyutası (məhsullardan gəlir; qarışıq ola bilməz).</summary>
    public string DraftCurrency => DraftLines.FirstOrDefault()?.Currency ?? "AZN";

    /// <summary>Depozit paneli yalnız siyahı rejimində və ödəniş pəncərəsi açıq deyilkən görünür.</summary>
    public bool ShowDepositPanel => ListMode && !ShowPaymentPrompt;

    [RelayCommand]
    private async Task ToggleNewOrderAsync()
    {
        ShowNewOrder = !ShowNewOrder;
        if (ShowNewOrder) await EnsureFormDataAsync();
    }

    /// <summary>Sifariş forması üçün müştəri/məhsul/əməkdaş siyahılarını təzələyir (#6 — hər dəfə yenilənir).</summary>
    public async Task EnsureFormDataAsync()
    {
        var selCustomerId = NewCustomer?.Id;
        var custs = await api.GetCustomersAsync(null);
        AllCustomers.Clear();
        if (custs is not null) foreach (var c in custs.Items) AllCustomers.Add(c);
        if (selCustomerId is { } id) NewCustomer = AllCustomers.FirstOrDefault(c => c.Id == id);

        var prods = await api.GetProductsAsync(null);
        AllProducts.Clear();
        if (prods is not null) foreach (var p in prods.Items) AllProducts.Add(p);

        if (CanChooseCreator)
        {
            var emps = await api.GetEmployeesAsync(null);
            AllEmployees.Clear();
            if (emps is not null) foreach (var e in emps.Items) AllEmployees.Add(e);
        }
    }

    [RelayCommand]
    private void ClearCreator() => SelectedCreator = null;

    /// <summary>Seçilmiş sifariş üçün detal kartı VM-i yaradır (#21) — View kod-arxasından çağırılır.</summary>
    public OrderDetailViewModel? CreateDetail() =>
        SelectedDto is null ? null : new OrderDetailViewModel(api, SelectedDto);

    /// <summary>Verilmiş sifariş üçün detal kartı (sətir düyməsindən).</summary>
    public OrderDetailViewModel CreateDetailFor(OrderRow row) => new(api, row.Dto);

    /// <summary>Kateqoriya-məhsul seçim pəncərəsi üçün VM (kod-arxasından açılır, #5).</summary>
    public ProductPickerViewModel CreatePicker() => new(api);

    /// <summary>#7 — checkbox ilə seçilmiş bir neçə qaralama sifarişi eyni anda təsdiqlə.</summary>
    [RelayCommand]
    private async Task ConfirmSelectedAsync()
    {
        var chosen = Orders.Where(r => r.IsSelected && r.IsDraft).ToList();
        if (chosen.Count == 0) { ERP.Desktop.AppNotify.Show("Təsdiq üçün qaralama sifariş seçin (checkbox)."); return; }
        int done = 0, fail = 0;
        foreach (var r in chosen)
        {
            var (ok, _) = await api.ConfirmOrderAsync(r.Id);
            if (ok) done++; else fail++;
        }
        Status = $"{done} sifariş təsdiqləndi" + (fail > 0 ? $", {fail} alınmadı" : "") + ".";
        ERP.Desktop.AppNotify.Show($"✓ {done} sifariş təsdiqləndi" + (fail > 0 ? $" ({fail} alınmadı)" : "") + ".");
        await LoadAsync();
    }

    /// <summary>Seçim pəncərəsindən gələn məhsulları sifariş sətirlərinə əlavə edir (say 1).</summary>
    public void AddPickedProducts(System.Collections.Generic.IEnumerable<ERP.Shared.Contracts.Products.ProductDto> picked)
    {
        int added = 0, skippedCurrency = 0;
        foreach (var p in picked)
        {
            if (DraftLines.Any(l => l.ProductId == p.Id)) continue;
            // #7 — bir sifarişdə valyuta qarışdırıla bilməz.
            if (DraftLines.Count > 0 && !string.Equals(p.Currency, DraftCurrency, StringComparison.OrdinalIgnoreCase))
            {
                skippedCurrency++;
                continue;
            }
            DraftLines.Add(Track(new DraftLine(p.Id, p.Name, 1, p.RentalPrice, currency: p.Currency)));
            added++;
        }
        OnPropertyChanged(nameof(DraftTotal));
        OnPropertyChanged(nameof(DraftCurrency));
        Status = added > 0 ? $"{added} məhsul əlavə olundu." : "Yeni məhsul seçilmədi.";
        if (skippedCurrency > 0)
            ERP.Desktop.AppNotify.Show($"⚠ {skippedCurrency} məhsul fərqli valyutada olduğu üçün əlavə olunmadı (sifariş valyutası: {DraftCurrency}).");
    }

    /// <summary>Sətir say/qiymət dəyişdikdə ümumi cəmi yeniləyir (#6).</summary>
    private DraftLine Track(DraftLine line)
    {
        line.PropertyChanged += (_, _) => OnPropertyChanged(nameof(DraftTotal));
        return line;
    }

    [RelayCommand]
    private void AddLine()
    {
        if (LineProduct is null || LineQuantity <= 0) { Status = "Məhsul və say seçin."; return; }
        if (LineUnitPrice < 0) { Status = "Qiymət mənfi ola bilməz."; return; }
        if (DraftLines.Any(l => l.ProductId == LineProduct.Id)) { Status = "Bu məhsul artıq əlavə olunub."; return; }

        // #7 — bir sifarişdə valyuta qarışdırıla bilməz.
        if (DraftLines.Count > 0 && !string.Equals(LineProduct.Currency, DraftCurrency, StringComparison.OrdinalIgnoreCase))
        {
            var msg = $"⚠ Bu məhsul {LineProduct.Currency} valyutadadır — sifariş {DraftCurrency} valyutadadır. Qarışdırıla bilməz.";
            Status = msg; ERP.Desktop.AppNotify.Show(msg);
            return;
        }

        // #18/#19 — seçilmiş anbarda kifayət qədər boş varmı? (ekran ortası xəbərdarlıq)
        if (LineWarehouse is not null && LineQuantity > LineWarehouse.Free)
        {
            var msg = $"⚠ '{LineWarehouse.WarehouseName}' anbarında boş yalnız {LineWarehouse.Free} ədəddir (istənilən: {LineQuantity}).";
            Status = msg;
            ERP.Desktop.AppNotify.Show(msg);
            return;
        }

        // #29/#30 — istifadəçinin daxil etdiyi (dəyişdirilə bilən) vahid qiymət istifadə olunur.
        DraftLines.Add(Track(new DraftLine(LineProduct.Id, LineProduct.Name, LineQuantity, LineUnitPrice,
            LineWarehouse?.WarehouseId, LineWarehouse?.WarehouseName, LineWarehouse?.Free ?? 0, LineProduct.Currency)));
        OnPropertyChanged(nameof(DraftTotal));
        OnPropertyChanged(nameof(DraftCurrency));
        LineQuantity = 1;
    }

    [RelayCommand]
    private void RemoveLine(DraftLine line)
    {
        DraftLines.Remove(line);
        OnPropertyChanged(nameof(DraftTotal));
        OnPropertyChanged(nameof(DraftCurrency));
    }

    /// <summary>#18 — qaralama sifarişi forma-panelə yükləyir (düzənləmək üçün).</summary>
    public async Task BeginEditAsync(OrderDto dto)
    {
        await EnsureFormDataAsync();
        _editingOrderId = dto.Id;
        // Məsul əməkdaşı qoru — düzənləmədə sıfırlanıb admin olmasın.
        _editCreatedByName = dto.CreatedByName;
        _editCreatedByRole = dto.CreatedByRole;
        if (CanChooseCreator && dto.CreatedByName is { } cn)
            SelectedCreator = AllEmployees.FirstOrDefault(e => e.FullName == cn);
        IsEditingOrder = true;
        ShowNewOrder = true;
        NewOrderType = dto.OrderType;
        NewCustomer = AllCustomers.FirstOrDefault(c => c.Id == dto.CustomerId);
        NewStartDate = new DateTimeOffset(dto.StartDate.ToDateTime(TimeOnly.MinValue));
        NewEndDate = new DateTimeOffset(dto.EndDate.ToDateTime(TimeOnly.MinValue));
        NewDeposit = dto.Deposit;
        DraftLines.Clear();
        foreach (var l in dto.Lines)
            DraftLines.Add(Track(new DraftLine(l.ProductId, l.ProductName, l.Quantity, l.UnitPrice,
                warehouseName: l.WarehouseName, currency: l.Currency)));
        OnPropertyChanged(nameof(DraftTotal));
        OnPropertyChanged(nameof(DraftCurrency));
        ERP.Desktop.AppNotify.Show($"Sifariş düzənlənir: {dto.OrderNumber}");
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
            // Məsul əməkdaş: dropdown dəyişilibsə yeni dəyər, yoxsa (düzənlədə) əvvəlki qorunur.
            CreatedByName: CanChooseCreator ? (SelectedCreator?.FullName ?? _editCreatedByName) : _editCreatedByName,
            CreatedByRole: CanChooseCreator ? (SelectedCreator?.Position ?? _editCreatedByRole) : _editCreatedByRole,
            OrderType: NewOrderType);

        var (ok, error, newId) = await api.CreateOrderReturningIdAsync(request);
        if (ok)
        {
            // #17 — depozit yalnız yaratma zamanı təyin olunur.
            if (NewDeposit > 0 && newId is { } oid)
                await api.SetOrderDepositAsync(oid, NewDeposit);

            // #18 — düzənləmə idisə, köhnə qaralamanı sil (əvəzləmə semantikası).
            if (_editingOrderId is { } oldId)
                await api.DeleteOrderAsync(oldId);

            // #B — "Bron et" seçilibsə, sifariş dərhal təsdiqlənir (rezerv edilir).
            var booked = false;
            if (SelectedCreateMode == "Bron et" && newId is { } bid)
            {
                var (bok, _) = await api.ConfirmOrderAsync(bid);
                booked = bok;
            }

            Status = booked ? "Sifariş bron edildi (rezerv)." : (_editingOrderId is not null ? "Sifariş yeniləndi (Qaralama)." : "Sifariş yaradıldı (Qaralama).");
            ERP.Desktop.AppNotify.Show(booked ? "✓ Sifariş BRON edildi (rezerv)." : (_editingOrderId is not null ? "✓ Sifariş yeniləndi." : "✓ Sifariş yaradıldı (Qaralama)."));
            _editingOrderId = null;
            _editCreatedByName = null;
            _editCreatedByRole = null;
            IsEditingOrder = false;
            DraftLines.Clear();
            NewCustomer = null;
            SelectedCreator = null;
            NewDeposit = 0;
            ShowNewOrder = false;
            OnPropertyChanged(nameof(DraftTotal));
            await LoadAsync();
        }
        else
        {
            Status = error ?? "Sifariş yaradılmadı.";
            ERP.Desktop.AppNotify.Show("⚠ " + (error ?? "Sifariş yaradılmadı."));
        }
    }

    [RelayCommand]
    private async Task LoadAsync()
    {
        // Yaratma rejimində siyahı deyil, forma dropdown-larını təzələ (#6).
        if (CreateMode) { await EnsureFormDataAsync(); return; }

        IsBusy = true;
        Status = "Yüklənir...";
        try
        {
            var result = await api.GetOrdersAsync(Search);
            Orders.Clear();
            if (result is not null)
                foreach (var o in result.Items)
                    if ((SelectedTypeFilter == "Hamısı" || o.OrderType == SelectedTypeFilter) && MatchesStatusGroup(o.Status))
                        Orders.Add(new OrderRow(o));
            Status = $"{Orders.Count} sifariş" + (SelectedTypeFilter == "Hamısı" ? "" : $" ({SelectedTypeFilter})");
        }
        catch (System.Exception ex)
        {
            Status = $"Xəta: {ex.Message}";
        }
        finally { IsBusy = false; }
    }

    /// <summary>Qaralamanı təhvil ver: əvvəlcə təsdiqlə (rezerv), sonra təhvil ver, ödəniş panelini aç.</summary>
    [RelayCommand]
    private async Task DeliverRowAsync(OrderRow? row)
    {
        row ??= Selected;
        if (row is null) return;
        var num = row.OrderNumber; var id = row.Id;

        if (row.IsDraft)
        {
            var (cok, cerr) = await api.ConfirmOrderAsync(id);
            if (!cok) { ERP.Desktop.AppNotify.Show("⚠ " + (cerr ?? "Təsdiqlənmədi.")); await LoadAsync(); return; }
        }

        var (ok, error) = await api.DeliverOrderAsync(id);
        Status = ok ? "Sifariş təhvil verildi." : error ?? "Təhvil verilmədi.";
        ERP.Desktop.AppNotify.Show(ok ? $"✓ Təhvil verildi: {num}" : "⚠ " + (error ?? "Təhvil verilmədi."));
        await LoadAsync();
        if (ok) await OpenPaymentPromptAsync(id, num, "Təhvil verildi");
    }

    /// <summary>Sifarişi ləğv et (qaralama/təsdiqlənmiş).</summary>
    [RelayCommand]
    private async Task CancelRowAsync(OrderRow? row)
    {
        row ??= Selected;
        if (row is null) return;
        var num = row.OrderNumber;
        var (ok, error) = await api.CancelOrderAsync(row.Id);
        Status = ok ? "Sifariş ləğv edildi." : error ?? "Ləğv edilmədi.";
        ERP.Desktop.AppNotify.Show(ok ? $"✓ Ləğv edildi: {num}" : "⚠ " + (error ?? "Ləğv edilmədi."));
        await LoadAsync();
    }

    /// <summary>Təhvil verilmiş sifarişi qaytar + ödəniş/hesablaşma panelini aç.</summary>
    [RelayCommand]
    private async Task ReturnRowAsync(OrderRow? row)
    {
        row ??= Selected;
        if (row is null) return;
        var num = row.OrderNumber; var id = row.Id;
        var (ok, error) = await api.ReturnOrderAsync(id);
        Status = ok ? "Sifariş qaytarıldı." : error ?? "Qaytarılmadı.";
        ERP.Desktop.AppNotify.Show(ok ? $"✓ Qaytarıldı: {num}" : "⚠ " + (error ?? "Qaytarılmadı."));
        await LoadAsync();
        if (ok) await OpenPaymentPromptAsync(id, num, "Qaytarıldı", isReturn: true);
    }

    /// <summary>"Fakturaya bax" — sifarişin fakturasını PDF kimi açır.</summary>
    [RelayCommand]
    private async Task ViewInvoiceRowAsync(OrderRow? row)
    {
        row ??= Selected;
        if (row is null) return;
        var invs = await api.GetInvoicesAsync(row.OrderNumber);
        var inv = invs?.Items.FirstOrDefault(i => i.OrderNumber == row.OrderNumber);
        if (inv is null) { ERP.Desktop.AppNotify.Show("Bu sifariş üçün faktura tapılmadı."); return; }
        var bytes = await api.GetInvoicePdfAsync(inv.Id);
        if (bytes is null) { ERP.Desktop.AppNotify.Show("PDF alınmadı."); return; }
        try
        {
            var path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"{inv.InvoiceNumber}.pdf");
            await System.IO.File.WriteAllBytesAsync(path, bytes);
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(path) { UseShellExecute = true });
            ERP.Desktop.AppNotify.Show($"📄 Faktura açıldı: {inv.InvoiceNumber}");
        }
        catch (System.Exception ex) { ERP.Desktop.AppNotify.Show("PDF açılmadı: " + ex.Message); }
    }

    /// <summary>#18 — qaralama sifarişi düzənlə: tərkibini forma-panelə yüklə.</summary>
    [RelayCommand]
    private async Task EditDraftAsync(OrderRow? row)
    {
        row ??= Selected;
        if (row is null) return;
        if (!row.IsDraft) { ERP.Desktop.AppNotify.Show("Yalnız qaralama sifarişi düzənlənə bilər."); return; }
        await BeginEditAsync(row.Dto);
    }

    [RelayCommand]
    private async Task SettleAsync()
    {
        if (SelectedDto is null) { Status = "Sifariş seçin."; return; }
        var (ok, error) = await api.SettleOrderAsync(SelectedDto.Id, DamageCharge, PenaltyCharge, SettlementNotes);
        Status = ok ? "Hesablaşma qeyd edildi — depozit qaytarması hesablandı." : error ?? "Hesablaşma alınmadı.";
        if (ok) { DamageCharge = PenaltyCharge = 0; SettlementNotes = null; }
        await LoadAsync();
    }
}
