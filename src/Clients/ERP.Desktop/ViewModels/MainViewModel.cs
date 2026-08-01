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
/// Əsas pəncərə ViewModel-i — sol tree-menyu (qruplaşdırılmış) + sağ tab iş sahəsi.
/// Bölmə klik olunanda tab açılır (varsa seçilir). Bütün alt-ekranlar eyni
/// autentifikasiya olunmuş ErpApiClient-i bölüşür (TDD §31 — API-first).
/// </summary>
public partial class MainViewModel : ViewModelBase
{
    private readonly Action _onLogout;

    // Bölmə açarı → (başlıq, VM). Tab açılanda buradan tapılır.
    private readonly Dictionary<string, (string title, ViewModelBase vm)> _sections;

    public ObservableCollection<NavGroup> Menu { get; } = [];
    public ObservableCollection<WorkspaceTab> Tabs { get; } = [];
    [ObservableProperty] private WorkspaceTab? _selectedTab;

    public string Title => "ERP";
    public string CurrentUser { get; }
    public bool CanManageUsers { get; }
    public bool CanViewAudit { get; }

    public MainViewModel(ErpApiClient api, AuthResponse auth, Action onLogout,
        IReadOnlyList<FieldPermissionDto>? fieldPermissions = null)
    {
        _onLogout = onLogout;
        CurrentUser = $"{auth.FullName} ({auth.Role})";
        CanManageUsers = auth.Permissions.Contains("users.manage");
        CanViewAudit = auth.Permissions.Contains("audit.view");

        bool CanViewField(string key)
        {
            var rule = fieldPermissions?.FirstOrDefault(p => p.FieldKey == key);
            return rule is null ? auth.Role is "Admin" or "Menecer" : rule.AllowedRoles.Contains(auth.Role);
        }

        // Alt-ekran VM-ləri (bir dəfə yaradılır, tab-larda təkrar istifadə olunur).
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

        _sections = new()
        {
            ["İdarə Paneli"] = ("İdarə Paneli", dashboard),
            ["Müştərilər"] = ("Müştərilər", customers),
            ["Məhsullar"] = ("Məhsullar", products),
            ["Sifarişlər"] = ("Sifarişlər", orders),
            ["Fakturalar"] = ("Fakturalar", invoices),
            ["Təchizatçılar"] = ("Təchizatçılar", suppliers),
            ["Alışlar"] = ("Alışlar", purchases),
            ["Maliyyə"] = ("Maliyyə", finance),
            ["İşçilər"] = ("Təmsilçilər", employees),
            ["Davamiyyət"] = ("Davamiyyət", attendance),
            ["Əməkhaqqı"] = ("Əməkhaqqı", payroll),
            ["Anbarlar"] = ("Anbarlar", warehouses),
            ["Stok"] = ("Anbar stokları", stock),
            ["Hesabatlar"] = ("Hesabatlar", reports),
            ["İcarə Təqvimi"] = ("İcarə Təqvimi", rentalCalendar),
            ["İstifadəçilər"] = ("İstifadəçilər", users),
            ["Sahə İcazələri"] = ("Sahə İcazələri", fieldPermissions_),
            ["Rollar"] = ("Rollar", roles),
            ["Audit Jurnalı"] = ("Audit Jurnalı", audit),
        };

        NavItem Item(string key, string label) => new(label, new RelayCommand(() => OpenSection(key)));

        Menu.Add(new NavGroup("İDARƏ PANELİ", [Item("İdarə Paneli", "Ümumi baxış")], isExpanded: true));
        Menu.Add(new NavGroup("KARTLAR", [Item("Müştərilər", "Müştərilər")], isExpanded: true));
        Menu.Add(new NavGroup("TƏMSİLÇİLƏR", [
            Item("İşçilər", "Təmsilçilər"),
            Item("Davamiyyət", "Davamiyyət"),
            Item("Əməkhaqqı", "Əməkhaqqı"),
        ]));
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
        ]));
        Menu.Add(new NavGroup("MALİYYƏ", [
            Item("Maliyyə", "Maliyyə / Kassa"),
            Item("Hesabatlar", "Hesabatlar"),
        ]));
        Menu.Add(new NavGroup("SİSTEM", [
            new NavItem("İstifadəçilər", new RelayCommand(() => OpenSection("İstifadəçilər")), CanManageUsers),
            new NavItem("Rollar", new RelayCommand(() => OpenSection("Rollar")), CanManageUsers),
            new NavItem("Sahə İcazələri", new RelayCommand(() => OpenSection("Sahə İcazələri")), CanManageUsers),
            new NavItem("Audit Jurnalı", new RelayCommand(() => OpenSection("Audit Jurnalı")), CanViewAudit),
        ]));

        // Açılışda idarə panelini aç.
        OpenSection("İdarə Paneli");
    }

    /// <summary>Bölməni tab kimi açır (açıqdırsa seçir) və məzmununu yükləyir.</summary>
    public void OpenSection(string key)
    {
        if (!_sections.TryGetValue(key, out var s)) return;

        var existing = Tabs.FirstOrDefault(t => t.Key == key);
        if (existing is null)
        {
            existing = new WorkspaceTab(key, s.title, s.vm, new RelayCommand<WorkspaceTab>(CloseTab));
            Tabs.Add(existing);
            LoadContent(s.vm);
        }
        SelectedTab = existing;
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
