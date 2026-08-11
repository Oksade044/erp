using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ERP.Mobile.Services;
using ERP.Mobile.Views;
using ERP.Shared.Contracts.Mobile;

namespace ERP.Mobile.ViewModels;

/// <summary>İşçi dashboard-u — yalnız cari işçinin göstəriciləri; karta toxununca siyahı açılır.</summary>
public partial class DashboardViewModel(MobileApiClient api, AppState state) : ObservableObject
{
    [ObservableProperty] private string _welcome = "Xoş gəlmisiniz";
    [ObservableProperty] private EmployeeDashboardDto? _data;
    [ObservableProperty] private ERP.Shared.Contracts.Representatives.RepresentativeLedgerDto? _debt;
    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private string? _status;

    /// <summary>Əsas ekranda borc mətni (mənfi balans = borclu).</summary>
    public string DebtText
    {
        get
        {
            if (Debt is null) return "Borc məlumatı yoxdur";
            var owed = Debt.Balance < 0 ? -Debt.Balance : 0;
            return owed > 0
                ? $"Qalan borcunuz: {owed:0.00} {Debt.Currency}"
                : "Borcunuz yoxdur ✓";
        }
    }
    public string DebtSubText => Debt is null ? "" : $"Bağlanan (sifarişlər): {Debt.TotalOrders:0.00} {Debt.Currency}";
    partial void OnDebtChanged(ERP.Shared.Contracts.Representatives.RepresentativeLedgerDto? value)
    {
        OnPropertyChanged(nameof(DebtText));
        OnPropertyChanged(nameof(DebtSubText));
    }

    public async Task LoadAsync()
    {
        Welcome = $"Xoş gəlmisiniz, {state.User?.FullName ?? "işçi"}";
        IsBusy = true;
        Status = null;
        try
        {
            Data = await api.GetMyDashboardAsync();
            Debt = await api.GetMyDebtAsync();
            if (Data is null) Status = "Məlumat yüklənmədi.";
        }
        catch (Exception ex) { Status = $"Xəta: {ex.Message}"; }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private Task RefreshAsync() => LoadAsync();

    /// <summary>"Borcu bağla" — yeni sifariş yaratma səhifəsinə keçir.</summary>
    [RelayCommand]
    private async Task GoNewOrderAsync() => await Shell.Current.GoToAsync("//neworder");
}
