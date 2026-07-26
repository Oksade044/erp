using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ERP.Desktop.Services;
using ERP.Shared.Contracts.Audit;
using ERP.Shared.Contracts.Invoices;
using ERP.Shared.Contracts.Orders;

namespace ERP.Desktop.ViewModels;

/// <summary>Sifariş detal kartı (#21) — bütün məlumatlar, ödənişlər, dəyişiklik tarixçəsi.</summary>
public partial class OrderDetailViewModel : ViewModelBase
{
    private readonly ErpApiClient _api;

    public OrderDto Order { get; }
    public ObservableCollection<OrderLineDto> Lines { get; } = [];
    public ObservableCollection<PaymentDto> Payments { get; } = [];
    public ObservableCollection<AuditLogDto> History { get; } = [];

    [ObservableProperty] private InvoiceDto? _invoice;
    [ObservableProperty] private string? _status;

    public string Title => $"Sifariş {Order.OrderNumber}";
    public bool HasInvoice => Invoice is not null;

    public OrderDetailViewModel(ErpApiClient api, OrderDto order)
    {
        _api = api;
        Order = order;
        foreach (var l in order.Lines) Lines.Add(l);
        _ = LoadAsync();
    }

    private async Task LoadAsync()
    {
        // Bu sifarişin fakturası + ödənişləri (nömrə üzrə axtar).
        try
        {
            var invs = await _api.GetInvoicesAsync(Order.OrderNumber);
            var inv = invs?.Items.FirstOrDefault(i => i.OrderNumber == Order.OrderNumber);
            if (inv is not null)
            {
                Invoice = inv;
                OnPropertyChanged(nameof(HasInvoice));
                foreach (var p in inv.Payments) Payments.Add(p);
            }
        }
        catch { /* faktura yoxdur */ }

        // Dəyişiklik tarixçəsi (audit — yalnız səlahiyyətli istifadəçidə gələcək).
        try
        {
            var audit = await _api.GetAuditLogsAsync(null, Order.Id);
            if (audit is not null) foreach (var a in audit.Items) History.Add(a);
        }
        catch { /* audit icazəsi yoxdur */ }

        Status = History.Count > 0
            ? $"{Payments.Count} ödəniş · {History.Count} tarixçə qeydi"
            : $"{Payments.Count} ödəniş";
    }

    [RelayCommand]
    private async Task OpenPdfAsync()
    {
        if (Invoice is null) { Status = "Bu sifariş üçün faktura yoxdur."; return; }
        var bytes = await _api.GetInvoicePdfAsync(Invoice.Id);
        if (bytes is null) { Status = "PDF alınmadı."; return; }
        var path = Path.Combine(Path.GetTempPath(), $"{Invoice.InvoiceNumber}.pdf");
        await File.WriteAllBytesAsync(path, bytes);
        Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
    }
}
