using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ERP.Desktop.Services;
using ERP.Shared.Contracts.Customers;
using ERP.Shared.Contracts.Invoices;

namespace ERP.Desktop.ViewModels;

/// <summary>Bir borc sətri — faktura borcu VƏ YA müştərinin ilkin (kart) borcu.</summary>
public sealed class DebtRow
{
    public string CustomerName { get; init; } = "";
    public string Reference { get; init; } = "";
    public decimal Total { get; init; }
    public decimal Paid { get; init; }
    public decimal Balance { get; init; }
    public string Currency { get; init; } = "AZN";
    public string Source { get; init; } = "";
    public Guid InvoiceId { get; init; }
    public CustomerDto? Customer { get; init; }  // ilkin borc üçün (müştəri kartı)
}

/// <summary>
/// Borclar — bizə borclu müştərilər: həm faktura qalıq borcları, həm də müştəri kartındakı
/// ilkin borc. Hər müştəri alt-alta; seçib ödəyərək borc azaldılır (#B).
/// </summary>
public partial class CustomerDebtsViewModel(ErpApiClient api) : ViewModelBase
{
    public ObservableCollection<DebtRow> Debts { get; } = [];
    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private string? _status;
    [ObservableProperty] private decimal _totalDebt;

    [ObservableProperty] private DebtRow? _selected;
    [ObservableProperty] private decimal _payAmount;
    public string[] PaymentMethods { get; } = ["Nağd", "Köçürmə", "Kart"];
    [ObservableProperty] private string _payMethod = "Nağd";

    partial void OnSelectedChanged(DebtRow? value) { if (value is not null) PayAmount = value.Balance; }

    [RelayCommand]
    private async Task LoadAsync()
    {
        IsBusy = true;
        Status = "Yüklənir...";
        try
        {
            Debts.Clear();

            // 1) Faktura qalıq borcları.
            var inv = await api.GetOutstandingAsync();
            if (inv is not null)
                foreach (var d in inv.Where(x => x.Balance > 0))
                    Debts.Add(new DebtRow
                    {
                        CustomerName = d.CustomerName, Reference = d.OrderNumber, Source = "Faktura",
                        Total = d.Total, Paid = d.Paid, Balance = d.Balance, Currency = d.Currency,
                        InvoiceId = d.InvoiceId
                    });

            // 2) Müştəri kartındakı ilkin borc (Debt > 0) — faktura ilə bağlı deyil.
            var custs = await api.GetCustomersAsync(null);
            if (custs is not null)
                foreach (var c in custs.Items.Where(x => x.Debt > 0))
                    Debts.Add(new DebtRow
                    {
                        CustomerName = c.Name, Reference = "İlkin borc (kart)", Source = "Kart",
                        Total = c.Debt, Paid = 0, Balance = c.Debt, Currency = c.DebtCurrency,
                        Customer = c
                    });

            // Müştəri adına görə alt-alta.
            var ordered = Debts.OrderBy(d => d.CustomerName).ThenByDescending(d => d.Balance).ToList();
            Debts.Clear();
            foreach (var d in ordered) Debts.Add(d);

            TotalDebt = Debts.Sum(d => d.Balance);
            Status = Debts.Count == 0 ? "Borclu müştəri yoxdur." : $"{Debts.Count} borc sətri";
        }
        catch (Exception ex) { Status = $"Xəta: {ex.Message}"; }
        finally { IsBusy = false; }
    }

    /// <summary>Seçilmiş borcu ödəniş qeyd edərək azaldır (faktura ödənişi VƏ YA kart borcu azaltma).</summary>
    [RelayCommand]
    private async Task PayAsync()
    {
        if (Selected is null) { ERP.Desktop.AppNotify.Show("Ödəmək üçün borc sətri seçin."); return; }
        if (PayAmount <= 0) { ERP.Desktop.AppNotify.Show("Ödəniş məbləği müsbət olmalıdır."); return; }

        bool ok; string? err;
        if (Selected.InvoiceId != Guid.Empty)
        {
            (ok, err) = await api.AddInvoicePaymentAsync(Selected.InvoiceId,
                new AddPaymentRequest(PayAmount, PayMethod, null, $"Borc ödənişi — {Selected.CustomerName}"));
        }
        else if (Selected.Customer is { } c)
        {
            var newDebt = Math.Max(0m, c.Debt - PayAmount);
            (ok, err) = await api.UpdateCustomerAsync(c.Id, new UpdateCustomerRequest(
                Name: c.Name, Phone: c.Phone, Email: c.Email, City: c.City, AddressLine: c.AddressLine,
                Notes: c.Notes, IsActive: c.IsActive, WhatsApp: c.WhatsApp,
                RepresentativeName: c.RepresentativeName, Debt: newDebt, DebtCurrency: c.DebtCurrency));
        }
        else { ERP.Desktop.AppNotify.Show("Bu borc üçün ödəniş mümkün deyil."); return; }

        ERP.Desktop.AppNotify.Show(ok
            ? $"✓ {Selected.CustomerName}: {PayAmount:0.00} ödənildi, borc azaldı."
            : "⚠ " + (err ?? "Ödəniş alınmadı."));
        if (ok) await LoadAsync();
    }
}
