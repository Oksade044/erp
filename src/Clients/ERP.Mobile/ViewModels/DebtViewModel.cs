using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ERP.Mobile.Services;
using ERP.Shared.Contracts.Representatives;

namespace ERP.Mobile.ViewModels;

/// <summary>Borcum (#17) — təmsilçinin cari balansı + defter tarixçəsi.</summary>
public partial class DebtViewModel(MobileApiClient api) : ObservableObject
{
    public ObservableCollection<RepresentativeEntryDto> Entries { get; } = [];

    [ObservableProperty] private decimal _balance;
    [ObservableProperty] private string _balanceText = "0.00 AZN";
    [ObservableProperty] private string _balanceHint = "";
    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private string? _status;

    public async Task LoadAsync()
    {
        IsBusy = true;
        try
        {
            var d = await api.GetMyDebtAsync();
            Entries.Clear();
            Balance = d?.Balance ?? 0;
            if (d is not null) foreach (var e in d.Entries) Entries.Add(e);
            BalanceText = $"{Balance:0.00} AZN";
            BalanceHint = Balance < 0
                ? "Borcunuz var — bu məbləğə uyğun sifariş yaratmalısınız."
                : Balance > 0 ? "Artıq (borcunuz yoxdur)." : "Borcunuz yoxdur.";
            Status = Entries.Count == 0 ? "Qeyd yoxdur." : null;
        }
        catch (System.Exception ex) { Status = $"Xəta: {ex.Message}"; }
        finally { IsBusy = false; }
    }

    [RelayCommand] private System.Threading.Tasks.Task RefreshAsync() => LoadAsync();
}
