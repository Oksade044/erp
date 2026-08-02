using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ERP.Desktop.Services;
using ERP.Shared.Contracts.Hr;

namespace ERP.Desktop.ViewModels;

/// <summary>Siyahıda çoxlu seçim üçün əməkhaqqı sətri (checkbox ilə toplu ödəniş — #5).</summary>
public partial class PayrollRow : ObservableObject
{
    public PayrollDto Dto { get; }
    [ObservableProperty] private bool _isSelected;

    public PayrollRow(PayrollDto dto) => Dto = dto;

    public Guid Id => Dto.Id;
    public string PayrollNumber => Dto.PayrollNumber;
    public string EmployeeName => Dto.EmployeeName;
    public int Year => Dto.Year;
    public int Month => Dto.Month;
    public decimal BaseSalary => Dto.BaseSalary;
    public decimal Bonus => Dto.Bonus;
    public decimal Deduction => Dto.Deduction;
    public decimal NetSalary => Dto.NetSalary;
    public decimal PaidAmount => Dto.PaidAmount;
    public decimal Remaining => Dto.Remaining;
    public string Currency => Dto.Currency;
    public string Status => Dto.Status;
    public DateOnly? PaidDate => Dto.PaidDate;
}

/// <summary>Ödəniş panelində bir işçinin sətri — nə qədər ödəniləcək + bonus (#20).</summary>
public partial class PayEntry : ObservableObject
{
    public PayrollRow Row { get; }
    [ObservableProperty] private decimal _amount;
    [ObservableProperty] private decimal _bonus;
    public PayEntry(PayrollRow row) { Row = row; _amount = row.Remaining; }

    public string EmployeeName => Row.EmployeeName;
    public string Period => $"{Row.Year}/{Row.Month:D2}";
    public decimal Remaining => Row.Remaining;
    public string Currency => Row.Currency;
}

/// <summary>
/// Əməkhaqqı ekranı — hesablamalar, checkbox ilə seçib "Seçilənləri ödə" düyməsi ilə açılan
/// paneldə hər işçiyə ayrıca məbləğ + bonus (#20 — sadələşdirilmiş, düymə əsaslı).
/// </summary>
public partial class PayrollViewModel(ErpApiClient api) : ViewModelBase
{
    /// <summary>#20 — ödəniş paneli (seçilən işçilər üçün, ekran ortası).</summary>
    public ObservableCollection<PayEntry> PayEntries { get; } = [];
    [ObservableProperty] private bool _showPayPanel;
    [ObservableProperty] private string _payPanelMethod = "Nağd";

    /// <summary>Seçilmiş işçiləri ödəniş panelinə yığır və panели açır.</summary>
    [RelayCommand]
    private void OpenPayPanel()
    {
        var chosen = Payrolls.Where(r => r.IsSelected).ToList();
        if (chosen.Count == 0) { ERP.Desktop.AppNotify.Show("Ödəmək üçün işçi(lər) seçin (checkbox)."); return; }
        PayEntries.Clear();
        foreach (var r in chosen) PayEntries.Add(new PayEntry(r));
        PayPanelMethod = "Nağd";
        ShowPayPanel = true;
    }

    [RelayCommand]
    private void CancelPayPanel() => ShowPayPanel = false;

    /// <summary>Paneldəki hər işçiyə öz məbləğini ödəyir + bonus varsa əlavə edir.</summary>
    [RelayCommand]
    private async Task SubmitPayAllAsync()
    {
        int paid = 0, bonuses = 0, fail = 0;
        var today = DateOnly.FromDateTime(DateTime.Now);
        foreach (var e in PayEntries.ToList())
        {
            if (e.Amount > 0)
            {
                var (ok, _) = await api.AddPayrollPaymentAsync(e.Row.Id,
                    new AddPayrollPaymentRequest(e.Amount, today, PayPanelMethod, "Ödəniş"));
                if (ok) paid++; else fail++;
            }
            if (e.Bonus > 0)
            {
                var (ok, _) = await api.AddPayrollBonusAsync(e.Row.Id,
                    new AddPayrollPaymentRequest(e.Bonus, today, PayPanelMethod, "Bonus"));
                if (ok) bonuses++; else fail++;
            }
        }
        ShowPayPanel = false;
        ERP.Desktop.AppNotify.Show($"✓ {paid} ödəniş" + (bonuses > 0 ? $", {bonuses} bonus" : "") + (fail > 0 ? $" ({fail} alınmadı)" : "") + ".");
        await LoadAsync();
    }

    public ObservableCollection<PayrollRow> Payrolls { get; } = [];
    public ObservableCollection<EmployeeDto> AllEmployees { get; } = [];

    /// <summary>Seçilmiş hesablamanın ödəniş tarixçəsi (installment + bonus).</summary>
    public ObservableCollection<PayrollPaymentDto> SelectedHistory { get; } = [];

    [ObservableProperty] private string? _search;
    partial void OnSearchChanged(string? value) => DebounceReload(LoadAsync);
    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private string? _status;

    [ObservableProperty] private PayrollRow? _selected;
    partial void OnSelectedChanged(PayrollRow? value)
    {
        SelectedHistory.Clear();
        if (value is not null)
        {
            foreach (var p in value.Dto.Payments) SelectedHistory.Add(p);
            PayAmount = value.Remaining; // default: qalıq borc
        }
        OnPropertyChanged(nameof(SelectedSummary));
    }

    /// <summary>Seçilmiş işçinin xülasəsi (net / ödənilmiş / qalıq).</summary>
    public string SelectedSummary => Selected is null
        ? "Hesablama seçin — ödəniş tarixçəsi burada görünəcək."
        : $"{Selected.EmployeeName} — {Selected.Year}/{Selected.Month:D2}: "
          + $"Net {Selected.NetSalary:0.00} {Selected.Currency}, "
          + $"ödənilmiş {Selected.PaidAmount:0.00}, qalıq {Selected.Remaining:0.00}.";

    // Yeni hesablama forması
    [ObservableProperty] private EmployeeDto? _newEmployee;
    [ObservableProperty] private int _newYear = DateTime.Now.Year;
    [ObservableProperty] private int _newMonth = DateTime.Now.Month;
    [ObservableProperty] private decimal _newBonus;
    [ObservableProperty] private decimal _newDeduction;

    // Ödəniş / bonus forması
    public string[] PaymentMethods { get; } = ["Nağd", "Köçürmə", "Kart"];
    [ObservableProperty] private decimal _payAmount;
    [ObservableProperty] private string _payMethod = "Nağd";
    [ObservableProperty] private string? _payNote;
    [ObservableProperty] private decimal _bonusAmount;
    [ObservableProperty] private string? _bonusNote;

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

            var selId = Selected?.Id;
            var result = await api.GetPayrollsAsync(Search);
            Payrolls.Clear();
            if (result is not null)
                foreach (var p in result.Items) Payrolls.Add(new PayrollRow(p));
            // Seçimi qoru (ödəniş sonrası tarixçə yenilənsin).
            if (selId is { } id) Selected = Payrolls.FirstOrDefault(r => r.Id == id);
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
        if (NewEmployee is null) { ERP.Desktop.AppNotify.Show("İşçi seçin."); return; }

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
                ERP.Desktop.AppNotify.Show($"✓ Əməkhaqqı hesablandı: {NewEmployee.FullName} ({NewYear}/{NewMonth:D2})");
                NewBonus = NewDeduction = 0;
                await LoadAsync();
            }
            else { Status = error ?? "Hesablanmadı."; ERP.Desktop.AppNotify.Show(Status); }
        }
        finally { IsBusy = false; }
    }

    /// <summary>Qalıq borcu tam ödəyir (Maliyyəyə məxaric yazılır).</summary>
    [RelayCommand]
    private async Task PayAsync()
    {
        if (Selected is null) { ERP.Desktop.AppNotify.Show("Hesablama seçin."); return; }
        var (ok, error) = await api.PayPayrollAsync(Selected.Id);
        Status = ok ? "Tam ödənildi — Maliyyəyə məxaric yazıldı." : error ?? "Ödənilmədi.";
        ERP.Desktop.AppNotify.Show(ok ? $"✓ Tam ödənildi: {Selected.EmployeeName}" : Status);
        await LoadAsync();
    }

    /// <summary>Hissə-hissə ödəniş (installment) — daxil edilən məbləğ qədər.</summary>
    [RelayCommand]
    private async Task PayInstallmentAsync()
    {
        if (Selected is null) { ERP.Desktop.AppNotify.Show("Hesablama seçin."); return; }
        if (PayAmount <= 0) { ERP.Desktop.AppNotify.Show("Ödəniş məbləği müsbət olmalıdır."); return; }

        var (ok, error) = await api.AddPayrollPaymentAsync(Selected.Id,
            new AddPayrollPaymentRequest(PayAmount, DateOnly.FromDateTime(DateTime.Now), PayMethod, PayNote));
        Status = ok ? $"Hissə ödəniş: {PayAmount:0.00} ({PayMethod})." : error ?? "Ödəniş alınmadı.";
        ERP.Desktop.AppNotify.Show(ok ? $"✓ Hissə ödəniş: {PayAmount:0.00} — {Selected.EmployeeName}" : Status);
        if (ok) { PayNote = null; await LoadAsync(); }
    }

    /// <summary>Aylıq bonus əlavəsi (net maaşı artırır + Maliyyəyə məxaric).</summary>
    [RelayCommand]
    private async Task AddBonusAsync()
    {
        if (Selected is null) { ERP.Desktop.AppNotify.Show("Hesablama seçin."); return; }
        if (BonusAmount <= 0) { ERP.Desktop.AppNotify.Show("Bonus məbləği müsbət olmalıdır."); return; }

        var (ok, error) = await api.AddPayrollBonusAsync(Selected.Id,
            new AddPayrollPaymentRequest(BonusAmount, DateOnly.FromDateTime(DateTime.Now), PayMethod, BonusNote));
        Status = ok ? $"Bonus verildi: {BonusAmount:0.00}." : error ?? "Bonus alınmadı.";
        ERP.Desktop.AppNotify.Show(ok ? $"✓ Bonus: {BonusAmount:0.00} — {Selected.EmployeeName}" : Status);
        if (ok) { BonusAmount = 0; BonusNote = null; await LoadAsync(); }
    }

    /// <summary>
    /// Checkbox ilə seçilmiş bir neçə hesablamaya ödəniş — daxil edilən məbləğ hər birinə tətbiq olunur
    /// (məbləğ 0-dırsa hər birinin qalıq borcu tam ödənilir).
    /// </summary>
    [RelayCommand]
    private async Task PaySelectedAsync()
    {
        var chosen = Payrolls.Where(r => r.IsSelected && r.Remaining > 0).ToList();
        if (chosen.Count == 0) { ERP.Desktop.AppNotify.Show("Ödəniş üçün heç bir hesablama seçilməyib (checkbox)."); return; }

        int done = 0, fail = 0;
        var today = DateOnly.FromDateTime(DateTime.Now);
        foreach (var r in chosen)
        {
            var amount = PayAmount > 0 ? Math.Min(PayAmount, r.Remaining) : r.Remaining;
            var (ok, _) = await api.AddPayrollPaymentAsync(r.Id,
                new AddPayrollPaymentRequest(amount, today, PayMethod, PayNote ?? "Toplu ödəniş"));
            if (ok) done++; else fail++;
        }
        Status = $"{done} işçiyə ödəniş edildi" + (fail > 0 ? $", {fail} alınmadı" : "") + ".";
        ERP.Desktop.AppNotify.Show($"✓ {done} işçiyə ödəniş edildi" + (fail > 0 ? $" ({fail} alınmadı)" : "") + ".");
        await LoadAsync();
    }
}
