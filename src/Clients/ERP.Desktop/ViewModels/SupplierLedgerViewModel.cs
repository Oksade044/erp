using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ERP.Desktop.Services;
using ERP.Shared.Contracts.Suppliers;

namespace ERP.Desktop.ViewModels;

/// <summary>
/// Təchizatçı defteri (#15) — borc/ödəniş, danışıq və sənəd tarixçəsi + qalıq borc.
/// Admin/menecer təchizatçı ilə bütün əlaqəni bir yerdə izləyir (nəzarət).
/// </summary>
public partial class SupplierLedgerViewModel : ViewModelBase
{
    private readonly ErpApiClient _api;
    private readonly Guid _supplierId;

    public string SupplierName { get; }
    public string[] EntryTypes { get; } = ["Borc", "Ödəniş", "Danışıq", "Sənəd"];

    public ObservableCollection<SupplierLedgerEntryDto> Entries { get; } = [];

    [ObservableProperty] private decimal _totalDebt;
    [ObservableProperty] private decimal _totalPaid;
    [ObservableProperty] private decimal _balance;
    [ObservableProperty] private string? _status;

    // Yeni qeyd forması
    [ObservableProperty] private string _newType = "Borc";
    [ObservableProperty] private decimal _newAmount;
    [ObservableProperty] private DateTimeOffset _newDate = DateTimeOffset.Now;
    [ObservableProperty] private string? _newDescription;

    public SupplierLedgerViewModel(ErpApiClient api, Guid supplierId, string supplierName)
    {
        _api = api;
        _supplierId = supplierId;
        SupplierName = supplierName;
        _ = LoadAsync();
    }

    private async Task LoadAsync()
    {
        Status = "Yüklənir...";
        try
        {
            var ledger = await _api.GetSupplierLedgerAsync(_supplierId);
            Entries.Clear();
            if (ledger is not null)
            {
                foreach (var e in ledger.Entries) Entries.Add(e);
                TotalDebt = ledger.TotalDebt;
                TotalPaid = ledger.TotalPaid;
                Balance = ledger.Balance;
            }
            Status = Entries.Count == 0 ? "Hələ qeyd yoxdur." : $"{Entries.Count} qeyd";
        }
        catch (Exception ex) { Status = $"Xəta: {ex.Message}"; }
    }

    [RelayCommand]
    private async Task AddEntryAsync()
    {
        var isMoney = NewType is "Borc" or "Ödəniş";
        if (isMoney && NewAmount <= 0) { Status = "Borc/ödəniş üçün məbləğ 0-dan böyük olmalıdır."; return; }
        if (NewType == "Danışıq" && string.IsNullOrWhiteSpace(NewDescription)) { Status = "Danışıq qeydi üçün mətn tələb olunur."; return; }

        var req = new AddSupplierEntryRequest(
            Type: NewType,
            Amount: isMoney ? NewAmount : 0m,
            Date: DateOnly.FromDateTime(NewDate.DateTime),
            Description: NewDescription);

        var (ok, err) = await _api.AddSupplierEntryAsync(_supplierId, req);
        if (!ok) { Status = err ?? "Qeyd əlavə olunmadı."; return; }

        NewAmount = 0; NewDescription = null;
        await LoadAsync();
    }

    [RelayCommand]
    private Task RefreshAsync() => LoadAsync();
}
