using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ERP.Desktop.Services;
using ERP.Shared.Contracts.Representatives;

namespace ERP.Desktop.ViewModels;

/// <summary>
/// Təmsilçi borcları (#16-18) — hər təmsilçinin cari balansı (mənfi = borclu),
/// admin borc təyin edir, seçilən təmsilçinin defter tarixçəsi.
/// </summary>
public partial class RepresentativesViewModel(ErpApiClient api) : ViewModelBase
{
    public ObservableCollection<RepresentativeBalanceDto> Balances { get; } = [];
    public ObservableCollection<RepresentativeEntryDto> Entries { get; } = [];

    [ObservableProperty] private RepresentativeBalanceDto? _selected;
    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private string? _status;
    [ObservableProperty] private decimal _ledgerBalance;

    // Borc təyin etmə forması
    [ObservableProperty] private string? _newRepName;
    [ObservableProperty] private decimal _newDebtAmount;
    [ObservableProperty] private DateTimeOffset _newDebtDate = DateTimeOffset.Now;
    [ObservableProperty] private string? _newDebtNote;

    partial void OnSelectedChanged(RepresentativeBalanceDto? value)
    {
        if (value is not null) { NewRepName = value.RepresentativeName; _ = LoadLedgerAsync(value.RepresentativeName); }
    }

    [RelayCommand]
    private async Task LoadAsync()
    {
        IsBusy = true;
        Status = "Yüklənir...";
        try
        {
            var list = await api.GetRepresentativeBalancesAsync();
            Balances.Clear();
            if (list is not null) foreach (var b in list) Balances.Add(b);
            Status = Balances.Count == 0 ? "Hələ təmsilçi borcu yoxdur." : $"{Balances.Count} təmsilçi";
        }
        catch (Exception ex) { Status = $"Xəta: {ex.Message}"; }
        finally { IsBusy = false; }
    }

    private async Task LoadLedgerAsync(string name)
    {
        try
        {
            var ledger = await api.GetRepresentativeLedgerAsync(name);
            Entries.Clear();
            if (ledger is not null)
            {
                foreach (var e in ledger.Entries) Entries.Add(e);
                LedgerBalance = ledger.Balance;
            }
        }
        catch (Exception ex) { Status = $"Xəta: {ex.Message}"; }
    }

    [RelayCommand]
    private async Task AssignDebtAsync()
    {
        if (string.IsNullOrWhiteSpace(NewRepName)) { Status = "Təmsilçi adı tələb olunur."; return; }
        if (NewDebtAmount <= 0) { Status = "Borc məbləği 0-dan böyük olmalıdır."; return; }

        var (ok, err) = await api.AssignDebtAsync(new AssignDebtRequest(
            RepresentativeName: NewRepName!.Trim(),
            Amount: NewDebtAmount,
            Date: DateOnly.FromDateTime(NewDebtDate.DateTime),
            Description: NewDebtNote));
        if (!ok) { Status = err ?? "Borc təyin edilmədi."; return; }

        Status = $"Borc təyin edildi: {NewRepName} −{NewDebtAmount:0.00} AZN";
        NewDebtAmount = 0; NewDebtNote = null;
        await LoadAsync();
        await LoadLedgerAsync(NewRepName!.Trim());
    }
}
