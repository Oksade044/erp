using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ERP.Mobile.Services;
using ERP.Shared.Contracts.Mobile;

namespace ERP.Mobile.ViewModels;

/// <summary>Maliyyəm — yalnız cari işçinin dövriyyə statistikası.</summary>
public partial class FinanceViewModel(MobileApiClient api) : ObservableObject
{
    [ObservableProperty] private EmployeeFinanceDto? _data;
    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private string? _status;

    public async Task LoadAsync()
    {
        IsBusy = true;
        try
        {
            Data = await api.GetMyFinanceAsync();
            if (Data is null) Status = "Məlumat yüklənmədi.";
        }
        catch (Exception ex) { Status = $"Xəta: {ex.Message}"; }
        finally { IsBusy = false; }
    }

    [RelayCommand] private Task RefreshAsync() => LoadAsync();
}
