using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ERP.Mobile.Services;

namespace ERP.Mobile.ViewModels;

/// <summary>Profil — işçi məlumatları (yalnız oxu) + çıxış.</summary>
public partial class ProfileViewModel(AppState state) : ObservableObject
{
    public string FullName => state.User?.FullName ?? "-";
    public string Username => state.User?.Username ?? "-";
    public string Role => state.User?.Role ?? "-";
    public string Server => state.BaseUrl;

    [RelayCommand]
    private void Logout()
    {
        state.Clear();
        App.GoToLogin();
    }
}
