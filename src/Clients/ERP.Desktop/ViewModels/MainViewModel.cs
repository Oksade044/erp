using System;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ERP.Desktop.Services;
using ERP.Shared.Contracts.Auth;

namespace ERP.Desktop.ViewModels;

/// <summary>
/// Əsas pəncərə ViewModel-i — yan naviqasiya və cari səhifə. Bütün alt-ekranlar eyni
/// (autentifikasiya olunmuş) ErpApiClient-i bölüşür (TDD §31 — API-first).
/// </summary>
public partial class MainViewModel : ViewModelBase
{
    private readonly DashboardViewModel _dashboard;
    private readonly CustomersViewModel _customers;
    private readonly ProductsViewModel _products;
    private readonly OrdersViewModel _orders;
    private readonly InvoicesViewModel _invoices;
    private readonly SuppliersViewModel _suppliers;
    private readonly PurchasesViewModel _purchases;
    private readonly FinanceViewModel _finance;
    private readonly UsersViewModel _users;
    private readonly Action _onLogout;

    [ObservableProperty] private ViewModelBase _current = null!;
    [ObservableProperty] private string _selectedSection = "İdarə Paneli";

    public string Title => "ERP — Toy Dekoru & Tədbir Avadanlığı İcarəsi";
    public string CurrentUser { get; }

    /// <summary>İstifadəçi idarəetməsi bölməsi yalnız users.manage icazəsi olanlara görünür.</summary>
    public bool CanManageUsers { get; }

    public MainViewModel(ErpApiClient api, AuthResponse auth, Action onLogout)
    {
        _onLogout = onLogout;
        CurrentUser = $"{auth.FullName} ({auth.Role})";
        CanManageUsers = auth.Permissions.Contains("users.manage");

        _dashboard = new DashboardViewModel(api);
        _customers = new CustomersViewModel(api);
        _products = new ProductsViewModel(api);
        _orders = new OrdersViewModel(api);
        _invoices = new InvoicesViewModel(api);
        _suppliers = new SuppliersViewModel(api);
        _purchases = new PurchasesViewModel(api);
        _finance = new FinanceViewModel(api);
        _users = new UsersViewModel(api);

        Current = _dashboard;
        _dashboard.LoadCommand.Execute(null);
    }

    [RelayCommand]
    private void Navigate(string section)
    {
        SelectedSection = section;
        Current = section switch
        {
            "Müştərilər" => _customers,
            "Məhsullar" => _products,
            "Sifarişlər" => _orders,
            "Fakturalar" => _invoices,
            "Təchizatçılar" => _suppliers,
            "Alışlar" => _purchases,
            "Maliyyə" => _finance,
            "İstifadəçilər" => _users,
            _ => _dashboard
        };

        switch (Current)
        {
            case DashboardViewModel dv: dv.LoadCommand.Execute(null); break;
            case CustomersViewModel c: c.LoadCommand.Execute(null); break;
            case ProductsViewModel p: p.LoadCommand.Execute(null); break;
            case OrdersViewModel o: o.LoadCommand.Execute(null); break;
            case InvoicesViewModel i: i.LoadCommand.Execute(null); break;
            case SuppliersViewModel s: s.LoadCommand.Execute(null); break;
            case PurchasesViewModel pu: pu.LoadCommand.Execute(null); break;
            case FinanceViewModel f: f.LoadCommand.Execute(null); break;
            case UsersViewModel u: u.LoadCommand.Execute(null); break;
        }
    }

    [RelayCommand]
    private void Logout() => _onLogout();
}
