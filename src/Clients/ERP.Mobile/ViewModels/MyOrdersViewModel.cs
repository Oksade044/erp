using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ERP.Mobile.Services;
using ERP.Mobile.Views;
using ERP.Shared.Contracts.Orders;

namespace ERP.Mobile.ViewModels;

/// <summary>Mənim sifarişlərim — gün/status süzgəci ilə; yalnız cari işçinin sifarişləri.</summary>
[QueryProperty(nameof(Filter), "filter")]
public partial class MyOrdersViewModel(MobileApiClient api) : ObservableObject
{
    public ObservableCollection<OrderDto> Orders { get; } = [];

    [ObservableProperty] private string _filter = "all";
    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private string? _status;

    public string[] Filters { get; } = ["all", "today-delivery", "today-return", "active", "pending"];

    public string Title => Filter switch
    {
        "today-delivery" => "Bu gün təhvil veriləcək",
        "today-return" => "Bu gün qaytarılacaq",
        "active" => "Aktiv sifarişlərim",
        "pending" => "Gözləyən sifarişlərim",
        _ => "Bütün sifarişlərim"
    };

    partial void OnFilterChanged(string value) { OnPropertyChanged(nameof(Title)); _ = LoadAsync(); }

    [RelayCommand]
    public async Task LoadAsync()
    {
        IsBusy = true;
        Status = null;
        try
        {
            var list = await api.GetMyOrdersAsync(Filter);
            Orders.Clear();
            foreach (var o in list) Orders.Add(o);
            Status = Orders.Count == 0 ? "Sifariş yoxdur." : $"{Orders.Count} sifariş";
        }
        catch (Exception ex) { Status = $"Xəta: {ex.Message}"; }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private async Task SetFilterAsync(string filter) { Filter = filter; }

    [RelayCommand]
    private async Task OpenAsync(OrderDto? order)
    {
        if (order is null) return;
        await Shell.Current.GoToAsync($"{nameof(OrderDetailPage)}?id={order.Id}");
    }
}
