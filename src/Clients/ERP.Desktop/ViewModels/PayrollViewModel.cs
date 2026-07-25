using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ERP.Desktop.Services;
using ERP.Shared.Contracts.Hr;

namespace ERP.Desktop.ViewModels;

/// <summary>Əməkhaqqı ekranı — hesablamaların siyahısı, yeni hesablama və ödəniş.</summary>
public partial class PayrollViewModel(ErpApiClient api) : ViewModelBase
{
    public ObservableCollection<PayrollDto> Payrolls { get; } = [];
    public ObservableCollection<EmployeeDto> AllEmployees { get; } = [];

    [ObservableProperty] private string? _search;

    /// <summary>Canlı axtarış — yazıldıqca süzülür (Enter da işləyir).</summary>
    partial void OnSearchChanged(string? value) => DebounceReload(LoadAsync);
    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private string? _status;
    [ObservableProperty] private PayrollDto? _selected;

    // Yeni hesablama forması
    [ObservableProperty] private EmployeeDto? _newEmployee;
    [ObservableProperty] private int _newYear = DateTime.Now.Year;
    [ObservableProperty] private int _newMonth = DateTime.Now.Month;
    [ObservableProperty] private decimal _newBonus;
    [ObservableProperty] private decimal _newDeduction;

    [RelayCommand]
    private async Task LoadAsync()
    {
        IsBusy = true;
        Status = "Yüklənir...";
        try
        {
            if (AllEmployees.Count == 0)
            {
                var emps = await api.GetEmployeesAsync(null);
                if (emps is not null) foreach (var e in emps.Items) AllEmployees.Add(e);
            }

            var result = await api.GetPayrollsAsync(Search);
            Payrolls.Clear();
            if (result is not null)
                foreach (var p in result.Items) Payrolls.Add(p);
            Status = $"{Payrolls.Count} hesablama";
        }
        catch (System.Exception ex)
        {
            Status = $"Xəta: {ex.Message}";
        }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private async Task AddAsync()
    {
        if (NewEmployee is null) { Status = "İşçi seçin."; return; }

        IsBusy = true;
        try
        {
            var (ok, error) = await api.CreatePayrollAsync(new CreatePayrollRequest(
                EmployeeId: NewEmployee.Id,
                Year: NewYear,
                Month: NewMonth,
                Bonus: NewBonus,
                Deduction: NewDeduction));

            if (ok)
            {
                Status = "Əməkhaqqı hesablandı.";
                NewBonus = NewDeduction = 0;
                await LoadAsync();
            }
            else Status = error ?? "Hesablanmadı.";
        }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private async Task PayAsync()
    {
        if (Selected is null) { Status = "Hesablama seçin."; return; }
        var (ok, error) = await api.PayPayrollAsync(Selected.Id);
        Status = ok ? "Ödənildi — Maliyyəyə məxaric yazıldı." : error ?? "Ödənilmədi.";
        await LoadAsync();
    }
}
