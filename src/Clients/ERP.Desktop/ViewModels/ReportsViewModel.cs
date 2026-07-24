using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ERP.Desktop.Services;
using ERP.Shared.Contracts.Reports;

namespace ERP.Desktop.ViewModels;

/// <summary>Hesabatlar ekranı — müştəri hesabatı və zədə/itki hesabatı.</summary>
public partial class ReportsViewModel(ErpApiClient api) : ViewModelBase
{
    public ObservableCollection<CustomerReportRowDto> Customers { get; } = [];
    public ObservableCollection<DamageReportRowDto> Damages { get; } = [];

    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private string? _status;

    [RelayCommand]
    private async Task LoadAsync()
    {
        IsBusy = true;
        Status = "Yüklənir...";
        try
        {
            Customers.Clear();
            var custs = await api.GetCustomerReportAsync();
            if (custs is not null) foreach (var c in custs) Customers.Add(c);

            Damages.Clear();
            var dmg = await api.GetDamageReportAsync();
            if (dmg is not null) foreach (var d in dmg) Damages.Add(d);

            Status = $"{Customers.Count} müştəri · {Damages.Count} zədə qeydi";
        }
        catch (Exception ex) { Status = $"Xəta: {ex.Message}"; }
        finally { IsBusy = false; }
    }
}
