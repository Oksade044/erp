using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ERP.Mobile.Services;
using ERP.Mobile.Views;
using ERP.Shared.Contracts.Mobile;

namespace ERP.Mobile.ViewModels;

/// <summary>İşçi dashboard-u — yalnız cari işçinin göstəriciləri; karta toxununca siyahı açılır.</summary>
public partial class DashboardViewModel(MobileApiClient api, AppState state) : ObservableObject
{
    [ObservableProperty] private string _welcome = "Xoş gəlmisiniz";
    [ObservableProperty] private EmployeeDashboardDto? _data;
    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private string? _status;

    public async Task LoadAsync()
    {
        Welcome = $"Xoş gəlmisiniz, {state.User?.FullName ?? "işçi"}";
        IsBusy = true;
        Status = null;
        try
        {
            Data = await api.GetMyDashboardAsync();
            if (Data is null) Status = "Məlumat yüklənmədi.";
        }
        catch (Exception ex) { Status = $"Xəta: {ex.Message}"; }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private Task RefreshAsync() => LoadAsync();

    /// <summary>Kartdan "mənim sifarişlərim" səhifəsinə süzgəclə keç.</summary>
    [RelayCommand]
    private async Task OpenOrdersAsync(string filter) =>
        await Shell.Current.GoToAsync($"//myorders?filter={filter}");
}
