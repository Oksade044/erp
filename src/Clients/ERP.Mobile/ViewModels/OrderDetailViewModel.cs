using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ERP.Mobile.Services;
using ERP.Shared.Contracts.Invoices;
using ERP.Shared.Contracts.Orders;

namespace ERP.Mobile.ViewModels;

/// <summary>Sifariş detalı — ERP ilə eyni məlumat; status dəyişmə, depozit, ödəniş (icazə daxilində).</summary>
[QueryProperty(nameof(OrderId), "id")]
public partial class OrderDetailViewModel(MobileApiClient api) : ObservableObject
{
    [ObservableProperty] private string? _orderId;
    [ObservableProperty] private OrderDto? _order;
    [ObservableProperty] private InvoiceDto? _invoice;
    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private string? _status;

    // Ödəniş / depozit / hesablaşma girişləri
    [ObservableProperty] private decimal _paymentAmount;
    [ObservableProperty] private string _paymentMethod = "Nağd";
    [ObservableProperty] private decimal _depositAmount;
    [ObservableProperty] private decimal _damageCharge;
    [ObservableProperty] private decimal _penaltyCharge;

    public ObservableCollection<OrderLineDto> Lines { get; } = [];
    public string[] PaymentMethods { get; } = ["Nağd", "Köçürmə", "Kart"];

    partial void OnOrderIdChanged(string? value) => _ = LoadAsync();

    public async Task LoadAsync()
    {
        if (!Guid.TryParse(OrderId, out var id)) return;
        IsBusy = true;
        try
        {
            Order = await api.GetOrderAsync(id);
            Lines.Clear();
            if (Order is not null) foreach (var l in Order.Lines) Lines.Add(l);
            var invoices = await api.GetInvoicesAsync(Order?.OrderNumber);
            Invoice = invoices.FirstOrDefault(i => i.OrderNumber == Order?.OrderNumber);
            if (Invoice is not null && PaymentAmount == 0) PaymentAmount = Invoice.Balance;
        }
        catch (Exception ex) { Status = $"Xəta: {ex.Message}"; }
        finally { IsBusy = false; }
    }

    private async Task RunAsync(Func<Task<(bool ok, string? error)>> action, string okMsg)
    {
        if (Order is null) return;
        IsBusy = true;
        try
        {
            var (ok, err) = await action();
            Status = ok ? okMsg : err;
            if (ok) await LoadAsync();
        }
        finally { IsBusy = false; }
    }

    [RelayCommand] private Task ConfirmAsync() => RunAsync(() => api.ConfirmOrderAsync(Order!.Id), "Təsdiqləndi.");
    [RelayCommand] private Task DeliverAsync() => RunAsync(() => api.DeliverOrderAsync(Order!.Id), "Təhvil verildi.");
    [RelayCommand] private Task ReturnAsync() => RunAsync(() => api.ReturnOrderAsync(Order!.Id), "Qaytarıldı.");
    [RelayCommand] private Task CancelAsync() => RunAsync(() => api.CancelOrderAsync(Order!.Id), "Ləğv edildi.");

    [RelayCommand]
    private Task SetDepositAsync() => RunAsync(() => api.SetDepositAsync(Order!.Id, DepositAmount), $"Depozit təyin edildi: {DepositAmount:0.00}");

    [RelayCommand]
    private Task SettleAsync() => RunAsync(() => api.SettleOrderAsync(Order!.Id, DamageCharge, PenaltyCharge, null),
        $"Hesablaşma: tutulma {(DamageCharge + PenaltyCharge):0.00}");

    [RelayCommand]
    private async Task AddPaymentAsync()
    {
        if (Invoice is null) { Status = "Faktura yoxdur."; return; }
        if (PaymentAmount <= 0) { Status = "Məbləğ 0-dan böyük olmalıdır."; return; }
        await RunAsync(() => api.AddPaymentAsync(Invoice!.Id, PaymentAmount, PaymentMethod, null),
            $"Ödəniş əlavə olundu: {PaymentAmount:0.00} ({PaymentMethod})");
    }

    [RelayCommand]
    private async Task OpenPdfAsync()
    {
        if (Invoice is null) return;
        await Launcher.Default.OpenAsync(api.InvoicePdfUrl(Invoice.Id));
    }
}
