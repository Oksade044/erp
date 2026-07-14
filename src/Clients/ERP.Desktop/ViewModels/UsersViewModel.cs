using System;
using System.Collections.ObjectModel;
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

    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private string? _status;

    // Yeni istifadəçi forması
    [ObservableProperty] private string? _newUsername;
    [ObservableProperty] private string? _newPassword;
    [ObservableProperty] private string? _newFullName;
    [ObservableProperty] private string _newRole = "Kassir";

    public string[] Roles { get; } = ["Admin", "Menecer", "Anbardar", "Kassir"];

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
            Status = $"{Users.Count} istifadəçi";
        }
        catch (Exception ex) { Status = $"Xəta (icazə yoxdur ola bilər): {ex.Message}"; }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private async Task AddAsync()
    {
        if (string.IsNullOrWhiteSpace(NewUsername) || string.IsNullOrWhiteSpace(NewPassword) || string.IsNullOrWhiteSpace(NewFullName))
        {
            Status = "İstifadəçi adı, parol və ad tələb olunur.";
            return;
        }

        IsBusy = true;
        try
        {
            var (ok, error) = await api.CreateUserAsync(new CreateUserRequest(
                NewUsername!, NewPassword!, NewFullName!, NewRole));
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
