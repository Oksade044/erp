using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ERP.Desktop.Services;
using ERP.Shared.Contracts.Invoices;
using ERP.Shared.Contracts.Reports;

namespace ERP.Desktop.ViewModels;

/// <summary>
/// Borclar — bizə borcu olan müştərilər (hansı fakturadan/sifarişdən nə qədər qalıq borc).
/// Hər müştərinin bütün borcları alt-alta; sətir seçib ödəniş edərək borcu azaltmaq olar (#B).
/// </summary>
public partial class CustomerDebtsViewModel(ErpApiClient api) : ViewModelBase
{
    public ObservableCollection<OutstandingInvoiceDto> Debts { get; } = [];
    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private string? _status;
    [ObservableProperty] private decimal _totalDebt;

    // Ödəniş forması (seçilmiş borc üçün).
    [ObservableProperty] private OutstandingInvoiceDto? _selected;
    [ObservableProperty] private decimal _payAmount;
    public string[] PaymentMethods { get; } = ["Nağd", "Köçürmə", "Kart"];
    [ObservableProperty] private string _payMethod = "Nağd";

    partial void OnSelectedChanged(OutstandingInvoiceDto? value) { if (value is not null) PayAmount = value.Balance; }

    [RelayCommand]
    private async Task LoadAsync()
    {
        IsBusy = true;
        Status = "Yüklənir...";
        try
        {
            var list = await api.GetOutstandingAsync();
            Debts.Clear();
            // Hər müştəri üçün borcları qruplaşdır (alt-alta) — müştəri adına, sonra qalığa görə.
            if (list is not null)
                foreach (var d in list.Where(x => x.Balance > 0)
                             .OrderBy(x => x.CustomerName).ThenByDescending(x => x.Balance))
                    Debts.Add(d);
            TotalDebt = Debts.Sum(d => d.Balance);
            Status = Debts.Count == 0
                ? "Borclu müştəri yoxdur — bütün fakturalar ödənilib."
                : $"{Debts.Count} borc sətri";
        }
        catch (Exception ex) { Status = $"Xəta: {ex.Message}"; }
        finally { IsBusy = false; }
    }

    /// <summary>Seçilmiş müştəri borcunu ödəniş qeyd edərək azaldır.</summary>
    [RelayCommand]
    private async Task PayAsync()
    {
        if (Selected is null) { ERP.Desktop.AppNotify.Show("Ödəmək üçün borc sətri seçin."); return; }
        if (PayAmount <= 0) { ERP.Desktop.AppNotify.Show("Ödəniş məbləği müsbət olmalıdır."); return; }
        if (Selected.InvoiceId == Guid.Empty) { ERP.Desktop.AppNotify.Show("Faktura tapılmadı."); return; }

        var (ok, err) = await api.AddInvoicePaymentAsync(Selected.InvoiceId,
            new AddPaymentRequest(PayAmount, PayMethod, null, $"Borc ödənişi — {Selected.CustomerName}"));
        ERP.Desktop.AppNotify.Show(ok
            ? $"✓ {Selected.CustomerName}: {PayAmount:0.00} ödənildi, borc azaldı."
            : "⚠ " + (err ?? "Ödəniş alınmadı."));
        if (ok) await LoadAsync();
    }
}
