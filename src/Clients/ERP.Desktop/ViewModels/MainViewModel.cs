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
        var customers = new CustomersViewModel(api);
        var products = new ProductsViewModel(api, canViewCost: CanViewField("product.cost"));
        var orders = new OrdersViewModel(api,
            canChooseCreator: auth.Role is "Admin" or "Menecer",
            canViewCreator: CanViewField("order.creator"));
        var invoices = new InvoicesViewModel(api);
        var suppliers = new SuppliersViewModel(api);
        var purchases = new PurchasesViewModel(api);
        var finance = new FinanceViewModel(api);
        var employees = new EmployeesViewModel(api, canViewSalary: CanViewField("employee.salary"));
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
            ["Müştərilər"] = ("\U0001F465", "Müştərilər", customers),
            ["Məhsullar"] = ("\U0001F4E6", "Məhsullar", products),
            ["Sifarişlər"] = ("\U0001F9FE", "Sifarişlər", orders),
            ["Fakturalar"] = ("\U0001F9FE", "Fakturalar", invoices),
            ["Təchizatçılar"] = ("\U0001F69A", "Təchizatçılar", suppliers),
            ["Alışlar"] = ("\U0001F6D2", "Alışlar", purchases),
            ["Maliyyə"] = ("\U0001F4B5", "Maliyyə", finance),
            ["İşçilər"] = ("\U0001F464", "Təmsilçilər", employees),
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

        Menu.Add(new NavGroup("İDARƏ PANELİ", [Item("İdarə Paneli", "Ümumi baxış")], isExpanded: true));
        Menu.Add(new NavGroup("KARTLAR", [Item("Müştərilər", "Müştərilər")], isExpanded: true));
        Menu.Add(new NavGroup("TƏMSİLÇİLƏR", [
            Item("İşçilər", "Təmsilçilər"),
            Item("Davamiyyət", "Davamiyyət"),
            Item("Əməkhaqqı", "Əməkhaqqı"),
        ], isExpanded: true));
        Menu.Add(new NavGroup("SATIŞ", [
            Item("Sifarişlər", "Sifarişlər / İcarə"),
            Item("Fakturalar", "Fakturalar"),
            Item("İcarə Təqvimi", "İcarə Təqvimi"),
        ], isExpanded: true));
        Menu.Add(new NavGroup("MALLAR", [
            Item("Məhsullar", "Məhsullar"),
            Item("Stok", "Anbar stokları"),
            Item("Anbarlar", "Anbarlar"),
            Item("Alışlar", "Alışlar"),
            Item("Təchizatçılar", "Təchizatçılar"),
        ], isExpanded: true));
        Menu.Add(new NavGroup("MALİYYƏ", [
            Item("Maliyyə", "Maliyyə / Kassa"),
            Item("Hesabatlar", "Hesabatlar"),
        ], isExpanded: true));
        Menu.Add(new NavGroup("SİSTEM", [
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
            LoadContent(s.vm);
        }
        SelectedTab = existing;
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
            case EmployeesViewModel v: v.LoadCommand.Execute(null); break;
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
