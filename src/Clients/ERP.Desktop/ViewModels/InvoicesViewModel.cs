using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ERP.Desktop.Services;
using ERP.Shared.Contracts.Invoices;

namespace ERP.Desktop.ViewModels;

/// <summary>Fakturalar ekranı — siyahı, ödəniş əlavəsi və PDF qaimə açma.</summary>
public partial class InvoicesViewModel(ErpApiClient api) : ViewModelBase
{
    public ObservableCollection<InvoiceDto> Invoices { get; } = [];

    [ObservableProperty] private string? _search;
    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private string? _status;
    [ObservableProperty] private InvoiceDto? _selected;

    // Ödəniş forması
    [ObservableProperty] private decimal _payAmount;
    [ObservableProperty] private string _payMethod = "Nağd";

    public string[] PaymentMethods { get; } = ["Nağd", "Köçürmə", "Kart"];

    [RelayCommand]
    private async Task LoadAsync()
    {
        IsBusy = true;
        Status = "Yüklənir...";
        try
        {
            var result = await api.GetInvoicesAsync(Search);
            Invoices.Clear();
            if (result is not null)
                foreach (var i in result.Items) Invoices.Add(i);
            Status = $"{Invoices.Count} faktura";
        }
        catch (Exception ex) { Status = $"Xəta: {ex.Message}"; }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private async Task AddPaymentAsync()
    {
        if (Selected is null) { Status = "Faktura seçin."; return; }
        if (PayAmount <= 0) { Status = "Məbləğ 0-dan böyük olmalıdır."; return; }

        var (ok, error) = await api.AddInvoicePaymentAsync(
            Selected.Id, new AddPaymentRequest(PayAmount, PayMethod));
        Status = ok ? "Ödəniş əlavə olundu." : error ?? "Ödəniş əlavə edilmədi.";
        if (ok) { PayAmount = 0; await LoadAsync(); }
    }

    [RelayCommand]
    private async Task OpenPdfAsync()
    {
        if (Selected is null) { Status = "Faktura seçin."; return; }

        var bytes = await api.GetInvoicePdfAsync(Selected.Id);
        if (bytes is null) { Status = "PDF alınmadı."; return; }

        var path = Path.Combine(Path.GetTempPath(), $"{Selected.InvoiceNumber}.pdf");
        await File.WriteAllBytesAsync(path, bytes);
        Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
        Status = $"PDF açıldı: {Selected.InvoiceNumber}";
    }
}
