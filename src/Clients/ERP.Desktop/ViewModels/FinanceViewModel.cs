using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ERP.Desktop.Services;
using ERP.Shared.Contracts.Finance;

namespace ERP.Desktop.ViewModels;

/// <summary>Maliyyə (Kassa) ekranı — mədaxil/məxaric siyahısı, xülasə kartları, yeni əməliyyat.</summary>
public partial class FinanceViewModel(ErpApiClient api) : ViewModelBase
{
    public ObservableCollection<TransactionDto> Transactions { get; } = [];

    [ObservableProperty] private string? _search;
    [ObservableProperty] private string? _filterType; // null=hamısı, "Mədaxil", "Məxaric"
    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private string? _status;

    // Xülasə (kassa)
    [ObservableProperty] private decimal _totalIncome;
    [ObservableProperty] private decimal _totalExpense;
    [ObservableProperty] private decimal _balance;

    // Yeni əməliyyat forması
    [ObservableProperty] private string _newTransactionType = "Mədaxil";
    [ObservableProperty] private string? _newCategory;
    [ObservableProperty] private decimal _newAmount;
    [ObservableProperty] private DateTimeOffset _newDate = DateTimeOffset.Now;
    [ObservableProperty] private string _newMethod = "Nağd";
    [ObservableProperty] private string? _newDescription;

    public string[] TransactionTypes { get; } = ["Mədaxil", "Məxaric"];
    public string[] Methods { get; } = ["Nağd", "Köçürmə", "Kart"];
    public string[] FilterTypes { get; } = ["Hamısı", "Mədaxil", "Məxaric"];

    [RelayCommand]
    private async Task LoadAsync()
    {
        IsBusy = true;
        Status = "Yüklənir...";
        try
        {
            var type = FilterType is "Mədaxil" or "Məxaric" ? FilterType : null;
            var result = await api.GetTransactionsAsync(Search, type);
            Transactions.Clear();
            if (result is not null)
                foreach (var t in result.Items) Transactions.Add(t);

            var summary = await api.GetCashFlowSummaryAsync();
            if (summary is not null)
            {
                TotalIncome = summary.TotalIncome;
                TotalExpense = summary.TotalExpense;
                Balance = summary.Balance;
            }
            Status = $"{Transactions.Count} əməliyyat";
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
        if (string.IsNullOrWhiteSpace(NewCategory)) { Status = "Kateqoriya tələb olunur."; return; }
        if (NewAmount <= 0) { Status = "Məbləğ 0-dan böyük olmalıdır."; return; }

        IsBusy = true;
        try
        {
            var (ok, error) = await api.CreateTransactionAsync(new CreateTransactionRequest(
                Type: NewTransactionType,
                Category: NewCategory!,
                Amount: NewAmount,
                Date: DateOnly.FromDateTime(NewDate.DateTime),
                Method: NewMethod,
                Description: NewDescription));

            if (ok)
            {
                Status = "Əməliyyat əlavə olundu.";
                NewCategory = NewDescription = null;
                NewAmount = 0;
                await LoadAsync();
            }
            else Status = error ?? "Əlavə edilmədi.";
        }
        finally { IsBusy = false; }
    }
}
