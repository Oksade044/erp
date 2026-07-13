using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ERP.Desktop.Services;
using ERP.Shared.Contracts.Orders;

namespace ERP.Desktop.ViewModels;

/// <summary>Sifarişlər ekranı — icarə sifarişlərinin siyahısı, təsdiq/ləğv.</summary>
public partial class OrdersViewModel(ErpApiClient api) : ViewModelBase
{
    public ObservableCollection<OrderDto> Orders { get; } = [];

    [ObservableProperty] private string? _search;
    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private string? _status;
    [ObservableProperty] private OrderDto? _selected;

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
}
