using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ERP.Desktop.Services;
using ERP.Shared.Contracts.Finance;

namespace ERP.Desktop.ViewModels;

/// <summary>Kassa əməliyyat növü — ad + istiqamət (Mədaxil/Məxaric).</summary>
public sealed record KassaOp(string Name, string Direction);

/// <summary>
/// Kassa (#4) — nağd əməliyyatlar. Tarix aralığı, sol daxil / sağ çıxan, altda cəmi.
/// Əməliyyat növləri, valyuta və əməliyyatı edən (Mərkəz/Təmsilçi).
/// </summary>
public partial class KassaViewModel(ErpApiClient api) : ViewModelBase
{
    public ObservableCollection<TransactionDto> Incoming { get; } = [];  // sol — daxil olan
    public ObservableCollection<TransactionDto> Outgoing { get; } = [];  // sağ — çıxan

    [ObservableProperty] private DateTimeOffset _from = DateTimeOffset.Now.AddDays(-30);
    [ObservableProperty] private DateTimeOffset _to = DateTimeOffset.Now;
    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private string? _status;

    [ObservableProperty] private decimal _totalIncome;
    [ObservableProperty] private decimal _totalExpense;
    [ObservableProperty] private decimal _balance;

    // Yeni əməliyyat forması
    public KassaOp[] Operations { get; } =
    [
        new("Kassa Nəğd Giriş", "Mədaxil"),
        new("Kassa Nəğd Çıxış", "Məxaric"),
        new("Tədiyyə Fişi", "Məxaric"),
        new("Digər Kassa Əməliyyatı (Giriş)", "Mədaxil"),
        new("Digər Kassa Əməliyyatı (Çıxış)", "Məxaric"),
    ];
    public string[] Currencies { get; } = ["AZN", "USD", "EUR"];
    public string[] Methods { get; } = ["Nağd", "Köçürmə", "Kart"];

    [ObservableProperty] private KassaOp? _newOp;
    [ObservableProperty] private decimal _newAmount;
    [ObservableProperty] private string _newCurrency = "AZN";
    [ObservableProperty] private string _newMethod = "Nağd";
    [ObservableProperty] private string _newPerformedBy = "Mərkəz";
    [ObservableProperty] private string? _newNote;
    [ObservableProperty] private DateTimeOffset _newDate = DateTimeOffset.Now;

    [RelayCommand]
    private async Task LoadAsync()
    {
        IsBusy = true;
        Status = "Yüklənir...";
        try
        {
            var result = await api.GetTransactionsAsync(null, null);
            var from = DateOnly.FromDateTime(From.DateTime);
            var to = DateOnly.FromDateTime(To.DateTime);

            Incoming.Clear();
            Outgoing.Clear();
            if (result is not null)
            {
                foreach (var t in result.Items.Where(t => t.Date >= from && t.Date <= to))
                {
                    if (t.Type == "Mədaxil") Incoming.Add(t);
                    else Outgoing.Add(t);
                }
            }
            TotalIncome = Incoming.Sum(t => t.Amount);
            TotalExpense = Outgoing.Sum(t => t.Amount);
            Balance = TotalIncome - TotalExpense;
            Status = $"{Incoming.Count} daxil, {Outgoing.Count} çıxan";
        }
        catch (Exception ex) { Status = $"Xəta: {ex.Message}"; }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private async Task AddAsync()
    {
        if (NewOp is null) { Status = "Əməliyyat növünü seçin."; return; }
        if (NewAmount <= 0) { Status = "Məbləğ 0-dan böyük olmalıdır."; return; }

        var (ok, err) = await api.CreateTransactionAsync(new CreateTransactionRequest(
            Type: NewOp.Direction,
            Category: NewOp.Name,
            Amount: NewAmount,
            Date: DateOnly.FromDateTime(NewDate.DateTime),
            Method: NewMethod,
            Description: NewNote,
            Currency: NewCurrency,
            PerformedBy: string.IsNullOrWhiteSpace(NewPerformedBy) ? "Mərkəz" : NewPerformedBy));

        if (!ok) { Status = err ?? "Əməliyyat əlavə edilmədi."; return; }
        Status = $"Əməliyyat əlavə olundu: {NewOp.Name} {NewAmount:0.00} {NewCurrency}";
        NewAmount = 0; NewNote = null;
        await LoadAsync();
    }
}
