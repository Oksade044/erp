using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ERP.Mobile.Services;

namespace ERP.Mobile.ViewModels;

/// <summary>Giriş ekranı — işçi yalnız admin yaratdığı hesabla daxil olur (qeydiyyat yoxdur).</summary>
public partial class LoginViewModel(MobileApiClient api, AppState state) : ObservableObject
{
    [ObservableProperty] private string? _username;
    [ObservableProperty] private string? _password;
    [ObservableProperty] private string _serverUrl = state.BaseUrl;
    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private string? _error;
    [ObservableProperty] private bool _showServerSettings;

    [RelayCommand]
    private void ToggleServerSettings() => ShowServerSettings = !ShowServerSettings;

    [RelayCommand]
    private async Task LoginAsync()
    {
        Error = null;
        if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Password))
        {
            Error = "İstifadəçi adı və şifrə daxil edin.";
            return;
        }

        IsBusy = true;
        try
        {
            // Server ünvanını yadda saxla (VPS-ə keçid üçün).
            if (!string.IsNullOrWhiteSpace(ServerUrl)) state.BaseUrl = ServerUrl.Trim().TrimEnd('/');

            var (ok, err) = await api.LoginAsync(Username!.Trim(), Password!);
            if (!ok) { Error = err; return; }

            Password = null;
            App.GoToMain();
        }
        finally { IsBusy = false; }
    }
}
