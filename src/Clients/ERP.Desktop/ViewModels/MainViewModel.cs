using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ERP.Desktop.Services;
using ERP.Shared.Contracts.Auth;
using ERP.Shared.Contracts.Settings;

namespace ERP.Desktop.ViewModels;

/// <summary>
/// Əsas pəncərə ViewModel-i — sol tree-menyu (ikonlu, qruplaşdırılmış) + sağ tab iş sahəsi.
/// Bölmə klik olunanda tab açılır (varsa seçilir) və menyuda vurğulanır.
/// </summary>
public partial class MainViewModel : ViewModelBase
{
    private readonly Action _onLogout;
    private readonly Dictionary<string, (string icon, string title, ViewModelBase vm)> _sections;
    private readonly List<NavItem> _allItems = [];

    public ObservableCollection<NavGroup> Menu { get; } = [];
    public ObservableCollection<WorkspaceTab> Tabs { get; } = [];
    [ObservableProperty] private WorkspaceTab? _selectedTab;

    public string Title => "ERP";
    public string CurrentUser { get; }
    public string UserInitial { get; }
    public bool CanManageUsers { get; }
    public bool CanViewAudit { get; }

    public MainViewModel(ErpApiClient api, AuthResponse auth, Action onLogout,
        IReadOnlyList<FieldPermissionDto>? fieldPermissions = null)
    {
        _onLogout = onLogout;
        CurrentUser = $"{auth.FullName} ({auth.Role})";
        UserInitial = string.IsNullOrWhiteSpace(auth.FullName) ? "?" : auth.FullName.Trim()[..1].ToUpperInvariant();
        CanManageUsers = auth.Permissions.Contains("users.manage");
        CanViewAudit = auth.Permissions.Contains("audit.view");

        bool CanViewField(string key)
        {
            var rule = fieldPermissions?.FirstOrDefault(p => p.FieldKey == key);
            return rule is null ? auth.Role is "Admin" or "Menecer" : rule.AllowedRoles.Contains(auth.Role);
        }

        var dashboard = new DashboardViewModel(api);
        var customers = new CustomersViewModel(api, createMode: false);
        var customersCreate = new CustomersViewModel(api, createMode: true);
        var products = new ProductsViewModel(api, canViewCost: CanViewField("product.cost"), createMode: false);
        var productsCreate = new ProductsViewModel(api, canViewCost: CanViewField("product.cost"), createMode: true);
        var canChoose = auth.Role is "Admin" or "Menecer";
        var canViewCr = CanViewField("order.creator");
        var orders = new OrdersViewModel(api, canChoose, canViewCr, createMode: false);
        var ordersCreate = new OrdersViewModel(api, canChoose, canViewCr, createMode: true);
        // #B — statusa görə ayrıca sifariş siyahıları (sol menyuda qruplar).
        var ordersDraft = new OrdersViewModel(api, canChoose, canViewCr, statusGroup: "Qaralama");
        var ordersBooked = new OrdersViewModel(api, canChoose, canViewCr, statusGroup: "Bron");
        var ordersDelivery = new OrdersViewModel(api, canChoose, canViewCr, statusGroup: "Aparıldı");
        var ordersReturned = new OrdersViewModel(api, canChoose, canViewCr, statusGroup: "Qaytarılanlar");
        var ordersCancelled = new OrdersViewModel(api, canChoose, canViewCr, statusGroup: "Ləğv");
        var invoices = new InvoicesViewModel(api);
        var suppliers = new SuppliersViewModel(api);
        var purchases = new PurchasesViewModel(api);
        var finance = new FinanceViewModel(api);
        var kassa = new KassaViewModel(api);
        var categories = new CategoriesViewModel(api);
        var employees = new EmployeesViewModel(api, canViewSalary: CanViewField("employee.salary"), createMode: false);
        var employeesCreate = new EmployeesViewModel(api, canViewSalary: CanViewField("employee.salary"), createMode: true);
        var representatives = new RepresentativesViewModel(api);
        var customerDebts = new CustomerDebtsViewModel(api);
        var attendance = new AttendanceViewModel(api);
        var payroll = new PayrollViewModel(api);
        var warehouses = new WarehousesViewModel(api);
        var stock = new StockViewModel(api);
        var reports = new ReportsViewModel(api);
        var rentalCalendar = new RentalCalendarViewModel(api);
        var users = new UsersViewModel(api);
        var fieldPermissions_ = new FieldPermissionsViewModel(api);
        var roles = new RolesViewModel(api);
        var audit = new AuditViewModel(api);

        // açar → (ikon, başlıq, VM)
        _sections = new()
        {
            ["İdarə Paneli"] = ("\U0001F4CA", "İdarə Paneli", dashboard),
            ["Müştərilər"] = ("\U0001F465", "Müştəri siyahısı", customers),
            ["Müştəri Yarat"] = ("\U00002795", "Yeni müştəri", customersCreate),
            ["Məhsullar"] = ("\U0001F4E6", "Məhsul siyahısı", products),
            ["Məhsul Yarat"] = ("\U00002795", "Yeni məhsul", productsCreate),
            ["Kateqoriyalar"] = ("\U0001F3F7", "Kateqoriyalar", categories),
            ["Sifarişlər"] = ("\U0001F9FE", "Bütün sifarişlər", orders),
            ["Yeni Sifariş"] = ("\U00002795", "İcarə / Satış yarat", ordersCreate),
            ["Sifariş: Qaralama"] = ("\U0001F4DD", "Qaralama sifarişlər", ordersDraft),
            ["Sifariş: Bron"] = ("\U0001F4C5", "Bron (rezerv) sifarişlər", ordersBooked),
            ["Sifariş: Aparıldı"] = ("\U0001F69A", "Aparılanlar", ordersDelivery),
            ["Sifariş: Qaytarılanlar"] = ("\U000021A9", "Qaytarılanlar", ordersReturned),
            ["Sifariş: Ləğv"] = ("\U0000274C", "Ləğv edilənlər", ordersCancelled),
            ["Fakturalar"] = ("\U0001F9FE", "Fakturalar", invoices),
            ["Təchizatçılar"] = ("\U0001F69A", "Təchizatçılar", suppliers),
            ["Alışlar"] = ("\U0001F6D2", "Alışlar", purchases),
            ["Maliyyə"] = ("\U0001F4B5", "Maliyyə", finance),
            ["Kassa"] = ("\U0001F4B0", "Kassa", kassa),
            ["İşçilər"] = ("\U0001F464", "Təmsilçi siyahısı", employees),
            ["Təmsilçi Yarat"] = ("\U00002795", "Yeni təmsilçi", employeesCreate),
            ["Hesablar"] = ("\U0001F4B0", "Hesablar — təmsilçilər", representatives),
            ["Borclar"] = ("\U0001F4B8", "Borclar — müştərilər", customerDebts),
            ["Davamiyyət"] = ("\U0001F552", "Davamiyyət", attendance),
            ["Əməkhaqqı"] = ("\U0001F4B3", "Əməkhaqqı", payroll),
            ["Anbarlar"] = ("\U0001F3ED", "Anbarlar", warehouses),
            ["Stok"] = ("\U0001F4CB", "Anbar stokları", stock),
            ["Hesabatlar"] = ("\U0001F4C8", "Hesabatlar", reports),
            ["İcarə Təqvimi"] = ("\U0001F4C5", "İcarə Təqvimi", rentalCalendar),
            ["İstifadəçilər"] = ("\U0001F511", "İstifadəçilər", users),
            ["Sahə İcazələri"] = ("\U0001F6E1", "Sahə İcazələri", fieldPermissions_),
            ["Rollar"] = ("\U0001F3AD", "Rollar", roles),
            ["Audit Jurnalı"] = ("\U0001F4DC", "Audit Jurnalı", audit),
        };

        NavItem Item(string key, string label, bool visible = true)
        {
            var icon = _sections.TryGetValue(key, out var s) ? s.icon : "•";
            var item = new NavItem(key, icon, label, new RelayCommand(() => OpenSection(key)), visible);
            _allItems.Add(item);
            return item;
        }

        // #B — sol menyu istifadəçinin istədiyi qruplaşma ilə: Kartlar / Satış / Mal / Maliyyə.
        Menu.Add(new NavGroup("İDARƏ PANELİ", [Item("İdarə Paneli", "Ümumi baxış")], isExpanded: true));
        Menu.Add(new NavGroup("🪪 KARTLAR", [
            Item("Müştəri Yarat", "➕ Müştəri əlavə et"),
            Item("Təmsilçi Yarat", "➕ Təmsilçi əlavə et"),
            Item("Müştərilər", "Müştərilər siyahısı"),
            Item("İşçilər", "Təmsilçilər siyahısı"),
        ], isExpanded: true));
        Menu.Add(new NavGroup("🛒 SATIŞ", [
            Item("Yeni Sifariş", "➕ İcarə / Satış yarat"),
            Item("Sifarişlər", "Bütün sifarişlər"),
            Item("Sifariş: Qaralama", "• Qaralamalar"),
            Item("Sifariş: Bron", "• Bron (rezerv)"),
            Item("Sifariş: Aparıldı", "• Aparılanlar"),
            Item("Sifariş: Qaytarılanlar", "• Qaytarılanlar"),
            Item("Sifariş: Ləğv", "• Ləğv edilənlər"),
            Item("Fakturalar", "Fakturalar"),
            Item("İcarə Təqvimi", "İcarə Təqvimi"),
        ], isExpanded: true));
        Menu.Add(new NavGroup("📦 MAL", [
            Item("Məhsul Yarat", "➕ Məhsul yarat"),
            Item("Məhsullar", "Məhsul siyahısı / stok"),
            Item("Kateqoriyalar", "Kateqoriyalar"),
            Item("Stok", "Anbar stokları"),
            Item("Anbarlar", "Anbarlar"),
            Item("Alışlar", "Alışlar"),
            Item("Təchizatçılar", "Təchizatçılar"),
        ], isExpanded: true));
        Menu.Add(new NavGroup("💰 MALİYYƏ", [
            Item("Kassa", "Kassa"),
            Item("Maliyyə", "Maliyyə hesabatlar"),
            Item("Hesabatlar", "Hesabatlar"),
            Item("Borclar", "Borclar (müştərilər)"),
            Item("Hesablar", "Hesablar (təmsilçilər)"),
            Item("Əməkhaqqı", "Əməkhaqqı"),
        ], isExpanded: true));
        Menu.Add(new NavGroup("⚙ SİSTEM", [
            Item("İstifadəçilər", "İstifadəçilər", CanManageUsers),
            Item("Rollar", "Rollar", CanManageUsers),
            Item("Sahə İcazələri", "Sahə İcazələri", CanManageUsers),
            Item("Audit Jurnalı", "Audit Jurnalı", CanViewAudit),
        ]));

        OpenSection("İdarə Paneli");
    }

    public void OpenSection(string key)
    {
        if (!_sections.TryGetValue(key, out var s)) return;

        var existing = Tabs.FirstOrDefault(t => t.Key == key);
        if (existing is null)
        {
            existing = new WorkspaceTab(key, s.icon, s.title, s.vm, new RelayCommand<WorkspaceTab>(CloseTab));
            Tabs.Add(existing);
        }
        SelectedTab = existing;
        // Hər açılışda məzmunu yenilə — məs. başqa bölmədə yaradılan sifariş siyahıda dərhal görünsün.
        LoadContent(s.vm);
    }

    partial void OnSelectedTabChanged(WorkspaceTab? value)
    {
        foreach (var it in _allItems)
            it.IsActive = value is not null && it.Key == value.Key;
    }

    private void CloseTab(WorkspaceTab? tab)
    {
        if (tab is null) return;
        var idx = Tabs.IndexOf(tab);
        Tabs.Remove(tab);
        if (SelectedTab == tab && Tabs.Count > 0)
            SelectedTab = Tabs[Math.Min(idx, Tabs.Count - 1)];
    }

    private static void LoadContent(ViewModelBase vm)
    {
        switch (vm)
        {
            case DashboardViewModel v: v.LoadCommand.Execute(null); break;
            case CustomersViewModel v: v.LoadCommand.Execute(null); break;
            case ProductsViewModel v: v.LoadCommand.Execute(null); break;
            case OrdersViewModel v: v.LoadCommand.Execute(null); break;
            case InvoicesViewModel v: v.LoadCommand.Execute(null); break;
            case SuppliersViewModel v: v.LoadCommand.Execute(null); break;
            case PurchasesViewModel v: v.LoadCommand.Execute(null); break;
            case FinanceViewModel v: v.LoadCommand.Execute(null); break;
            case KassaViewModel v: v.LoadCommand.Execute(null); break;
            case CategoriesViewModel v: v.LoadCommand.Execute(null); break;
            case EmployeesViewModel v: v.LoadCommand.Execute(null); break;
            case RepresentativesViewModel v: v.LoadCommand.Execute(null); break;
            case CustomerDebtsViewModel v: v.LoadCommand.Execute(null); break;
            case AttendanceViewModel v: v.LoadCommand.Execute(null); break;
            case PayrollViewModel v: v.LoadCommand.Execute(null); break;
            case WarehousesViewModel v: v.LoadCommand.Execute(null); break;
            case StockViewModel v: v.LoadCommand.Execute(null); break;
            case ReportsViewModel v: v.LoadCommand.Execute(null); break;
            case RentalCalendarViewModel v: v.LoadCommand.Execute(null); break;
            case UsersViewModel v: v.LoadCommand.Execute(null); break;
            case FieldPermissionsViewModel v: v.LoadCommand.Execute(null); break;
            case RolesViewModel v: v.LoadCommand.Execute(null); break;
            case AuditViewModel v: v.LoadCommand.Execute(null); break;
        }
    }

    [RelayCommand]
    private void Logout() => _onLogout();
}
