using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ERP.Desktop.Services;
using ERP.Shared.Contracts.Reports;

namespace ERP.Desktop.ViewModels;

/// <summary>İdarə paneli — əsas göstəricilər, top məhsullar, borclu fakturalar.</summary>
public partial class DashboardViewModel(ErpApiClient api) : ViewModelBase
{
    [ObservableProperty] private DashboardDto? _summary;
    [ObservableProperty] private ProfitLossDto? _profitLoss;
    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private string? _status;

    public ObservableCollection<TopProductDto> TopProducts { get; } = [];
    public ObservableCollection<OutstandingInvoiceDto> Outstanding { get; } = [];
    public ObservableCollection<MonthlyPointDto> MonthlyRevenue { get; } = [];
    public ObservableCollection<EmployeePerformanceRowDto> TopEmployees { get; } = [];
    public ObservableCollection<CustomerReportRowDto> TopCustomers { get; } = [];

    [RelayCommand]
    private async Task LoadAsync()
    {
        IsBusy = true;
        Status = "Yüklənir...";
        try
        {
            Summary = await api.GetDashboardAsync();

            TopProducts.Clear();
            var top = await api.GetTopProductsAsync(10);
            if (top is not null) foreach (var p in top) TopProducts.Add(p);

            Outstanding.Clear();
            var debts = await api.GetOutstandingAsync();
            if (debts is not null) foreach (var d in debts) Outstanding.Add(d);

            // #25 — top əməkdaşlar (cari ay dövriyyəsi) + ən aktiv müştərilər.
            TopEmployees.Clear();
            var emps = await api.GetEmployeePerformanceAsync();
            if (emps is not null) foreach (var e in emps) TopEmployees.Add(e);

            TopCustomers.Clear();
            var custReport = await api.GetCustomerReportAsync();
            if (custReport is not null)
                foreach (var c in custReport.OrderByDescending(x => x.OrderCount).Take(10)) TopCustomers.Add(c);

            // Mənfəət/Zərər (cari il) + aylıq gəlir analitikası.
            ProfitLoss = await api.GetProfitLossAsync();
            MonthlyRevenue.Clear();
            var monthly = await api.GetMonthlyRevenueAsync();
            if (monthly is not null) foreach (var p in monthly.Points) MonthlyRevenue.Add(p);

            Status = "Yeniləndi";
        }
        catch (Exception ex) { Status = $"Xəta: {ex.Message}"; }
        finally { IsBusy = false; }
    }
}
