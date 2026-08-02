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

    // #22 — vizual qrafiklər (sadə zolaq diaqramları, xarici kitabxana olmadan).
    public ObservableCollection<StatusBar> StatusBars { get; } = [];
    public ObservableCollection<MonthBar> MonthBars { get; } = [];
    public ObservableCollection<StatusBar> TopProductBars { get; } = [];

    private void BuildCharts()
    {
        StatusBars.Clear();
        if (Summary is { } s)
        {
            var items = new (string Label, int Count, string Color)[]
            {
                ("Qaralama", s.DraftOrders, "#868E96"),
                ("Rezervdə", s.ConfirmedOrders, "#1971C2"),
                ("Kirayədə", s.DeliveredOrders, "#2F9E44"),
                ("Qaytarılmış", s.ReturnedOrders, "#7048E8"),
                ("Ləğv", s.CancelledOrders, "#E03131"),
            };
            var max = System.Math.Max(1, items.Max(i => i.Count));
            foreach (var i in items)
                StatusBars.Add(new StatusBar(i.Label, i.Count.ToString(), i.Count / (double)max * 280.0, i.Color));
        }

        MonthBars.Clear();
        if (MonthlyRevenue.Count > 0)
        {
            var maxV = System.Math.Max(1m, MonthlyRevenue.Max(p => System.Math.Max(p.Income, p.Expense)));
            foreach (var p in MonthlyRevenue)
                MonthBars.Add(new MonthBar(p.Month.ToString(),
                    (double)(p.Income / maxV) * 130.0, (double)(p.Expense / maxV) * 130.0,
                    p.Income, p.Expense));
        }

        TopProductBars.Clear();
        if (TopProducts.Count > 0)
        {
            var max = System.Math.Max(1, TopProducts.Max(p => p.TotalQuantityRented));
            foreach (var p in TopProducts.Take(8))
                TopProductBars.Add(new StatusBar(p.ProductName, p.TotalQuantityRented.ToString(),
                    p.TotalQuantityRented / (double)max * 260.0, "#3B5BDB"));
        }
    }

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

            BuildCharts();
            Status = "Yeniləndi";
        }
        catch (Exception ex) { Status = $"Xəta: {ex.Message}"; }
        finally { IsBusy = false; }
    }
}

/// <summary>Üfüqi zolaq diaqram sətri (status/məhsul üçün).</summary>
public sealed record StatusBar(string Label, string Value, double BarWidth, string Color);

/// <summary>Aylıq gəlir/xərc üçün şaquli zolaq (iki sütun).</summary>
public sealed record MonthBar(string Month, double IncomeHeight, double ExpenseHeight, decimal Income, decimal Expense);
