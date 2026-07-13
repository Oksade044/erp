using System.Net.Http;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ERP.Desktop.Services;

namespace ERP.Desktop.ViewModels;

/// <summary>
/// Əsas pəncərə ViewModel-i — yan naviqasiya və cari səhifə. Bütün alt-ekranlar eyni
/// ErpApiClient-i bölüşür (TDD §31 — API-first).
/// </summary>
public partial class MainViewModel : ViewModelBase
{
    private readonly CustomersViewModel _customers;
    private readonly ProductsViewModel _products;
    private readonly OrdersViewModel _orders;
    private readonly InvoicesViewModel _invoices;

    [ObservableProperty] private ViewModelBase _current = null!;
    [ObservableProperty] private string _selectedSection = "Müştərilər";

    public string Title => "ERP — Toy Dekoru & Tədbir Avadanlığı İcarəsi";

    public MainViewModel()
    {
        // Sadə kompozisiya (Desktop-da DI konteyneri yoxdur — tək HttpClient bölüşülür).
        var http = new HttpClient { BaseAddress = new System.Uri("http://localhost:5080") };
        var api = new ErpApiClient(http);

        _customers = new CustomersViewModel(api);
        _products = new ProductsViewModel(api);
        _orders = new OrdersViewModel(api);
        _invoices = new InvoicesViewModel(api);

        Current = _customers;
        _customers.LoadCommand.Execute(null);
    }

    [RelayCommand]
    private void Navigate(string section)
    {
        SelectedSection = section;
        Current = section switch
        {
            "Məhsullar" => _products,
            "Sifarişlər" => _orders,
            "Fakturalar" => _invoices,
            _ => _customers
        };

        switch (Current)
        {
            case CustomersViewModel c: c.LoadCommand.Execute(null); break;
            case ProductsViewModel p: p.LoadCommand.Execute(null); break;
            case OrdersViewModel o: o.LoadCommand.Execute(null); break;
            case InvoicesViewModel i: i.LoadCommand.Execute(null); break;
        }
    }
}
