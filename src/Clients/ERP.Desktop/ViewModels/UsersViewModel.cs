using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ERP.Desktop.Services;
using ERP.Shared.Contracts.Users;

namespace ERP.Desktop.ViewModels;

/// <summary>İstifadəçilər ekranı — siyahı və yeni istifadəçi yaratma (yalnız Admin, API enforce edir).</summary>
public partial class UsersViewModel(ErpApiClient api) : ViewModelBase
{
    public ObservableCollection<UserDto> Users { get; } = [];
    [ObservableProperty] private UserDto? _selected;

    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private string? _status;

    /// <summary>Seçilmiş istifadəçini silir (soft delete; admin qorunur — server enforce edir).</summary>
    [RelayCommand]
    private async Task DeleteSelectedAsync()
    {
        if (Selected is null) { ERP.Desktop.AppNotify.Show("Silmək üçün istifadəçi seçin."); return; }
        var name = Selected.Username;
        var (ok, err) = await api.DeleteUserAsync(Selected.Id);
        ERP.Desktop.AppNotify.Show(ok ? $"İstifadəçi silindi: {name}" : err ?? "Silinmədi.");
        if (ok) await LoadAsync();
    }

    // Yeni istifadəçi forması
    [ObservableProperty] private string? _newUsername;
    [ObservableProperty] private string? _newPassword;
    [ObservableProperty] private string? _newFullName;
    [ObservableProperty] private string? _newRole;

    /// <summary>Rollar dinamikdir (#16) — API-dən yüklənir.</summary>
    public ObservableCollection<string> Roles { get; } = [];

    [RelayCommand]
    private async Task LoadAsync()
    {
        IsBusy = true;
        Status = "Yüklənir...";
        try
        {
            Users.Clear();
            var list = await api.GetUsersAsync();
            if (list is not null) foreach (var u in list) Users.Add(u);

            // Rolları dinamik gətir (dropdown üçün).
            if (Roles.Count == 0)
            {
                var roles = await api.GetRolesAsync();
                if (roles is not null) foreach (var r in roles) Roles.Add(r.Name);
                NewRole ??= Roles.Contains("Kassir") ? "Kassir" : Roles.FirstOrDefault();
            }
            Status = $"{Users.Count} istifadəçi";
        }
        catch (Exception ex) { Status = $"Xəta (icazə yoxdur ola bilər): {ex.Message}"; }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private async Task AddAsync()
    {
        if (string.IsNullOrWhiteSpace(NewUsername) || string.IsNullOrWhiteSpace(NewPassword)
            || string.IsNullOrWhiteSpace(NewFullName) || string.IsNullOrWhiteSpace(NewRole))
        {
            Status = "İstifadəçi adı, parol, ad və rol tələb olunur.";
            return;
        }

        IsBusy = true;
        try
        {
            var (ok, error) = await api.CreateUserAsync(new CreateUserRequest(
                NewUsername!, NewPassword!, NewFullName!, NewRole!));
            if (ok)
            {
                Status = "İstifadəçi yaradıldı.";
                NewUsername = NewPassword = NewFullName = null;
                await LoadAsync();
            }
            else Status = error ?? "Yaradılmadı.";
        }
        finally { IsBusy = false; }
    }
}
