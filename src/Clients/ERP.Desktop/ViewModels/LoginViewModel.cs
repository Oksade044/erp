using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ERP.Desktop.Services;
using ERP.Shared.Contracts.Auth;

namespace ERP.Desktop.ViewModels;

/// <summary>Giriş ekranı — istifadəçi adı/parol → JWT token alır (TDD §6).</summary>
public partial class LoginViewModel(ErpApiClient api, Action<AuthResponse> onSuccess) : ViewModelBase
{
    [ObservableProperty] private string _username = "";
    [ObservableProperty] private string _password = "";
    [ObservableProperty] private string? _error;
    [ObservableProperty] private bool _isBusy;

    [RelayCommand]
    private async Task LoginAsync()
    {
        Error = null;
        if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Password))
        {
            Error = "İstifadəçi adı və parol tələb olunur.";
            return;
        }

        IsBusy = true;
        try
        {
            var (auth, error) = await api.LoginAsync(Username.Trim(), Password);
            if (auth is not null)
            {
                api.SetBearerToken(auth.AccessToken);
                onSuccess(auth);
            }
            else Error = error ?? "Giriş alınmadı.";
        }
        finally { IsBusy = false; }
    }
}
