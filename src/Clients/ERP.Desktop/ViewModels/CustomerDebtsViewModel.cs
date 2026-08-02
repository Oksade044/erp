using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ERP.Desktop.Services;
using ERP.Shared.Contracts.Reports;

namespace ERP.Desktop.ViewModels;

/// <summary>
/// Borclar — bizə borcu olan müştərilər (hansı fakturadan/sifarişdən nə qədər qalıq borc).
/// Ödənilməmiş və qismən ödənilmiş fakturalar üzrə hesablanır (#B).
/// </summary>
public partial class CustomerDebtsViewModel(ErpApiClient api) : ViewModelBase
{
    public ObservableCollection<OutstandingInvoiceDto> Debts { get; } = [];
    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private string? _status;
    [ObservableProperty] private decimal _totalDebt;

    [RelayCommand]
    private async Task LoadAsync()
    {
        IsBusy = true;
        Status = "Yüklənir...";
        try
        {
            var list = await api.GetOutstandingAsync();
            Debts.Clear();
            if (list is not null)
                foreach (var d in list.Where(x => x.Balance > 0).OrderByDescending(x => x.Balance))
                    Debts.Add(d);
            TotalDebt = Debts.Sum(d => d.Balance);
            Status = Debts.Count == 0
                ? "Borclu müştəri yoxdur — bütün fakturalar ödənilib."
                : $"{Debts.Count} borclu müştəri — ümumi qalıq: {TotalDebt:0.00}";
        }
        catch (Exception ex) { Status = $"Xəta: {ex.Message}"; }
        finally { IsBusy = false; }
    }
}
