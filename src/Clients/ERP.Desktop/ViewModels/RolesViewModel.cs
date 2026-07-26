using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ERP.Desktop.Services;
using ERP.Shared.Contracts.Users;

namespace ERP.Desktop.ViewModels;

/// <summary>Rollar & icazələr (#16) — Admin rol yaradır və hər rol üçün icazələri təyin edir.</summary>
public partial class RolesViewModel(ErpApiClient api) : ViewModelBase
{
    public ObservableCollection<RoleDto> Roles { get; } = [];
    public ObservableCollection<PermissionCheck> Permissions { get; } = [];

    [ObservableProperty] private RoleDto? _selectedRole;
    [ObservableProperty] private string? _newRoleName;
    [ObservableProperty] private string? _status;
    [ObservableProperty] private bool _isEditable;

    private List<PermissionInfoDto> _catalog = [];

    partial void OnSelectedRoleChanged(RoleDto? value)
    {
        Permissions.Clear();
        if (value is null) { IsEditable = false; return; }

        foreach (var p in _catalog)
            Permissions.Add(new PermissionCheck(p.Key, p.Label, value.Permissions.Contains(p.Key)));

        // Admin rolu qorunur (icazələri azaldıla bilməz).
        IsEditable = value.Name != "Admin";
    }

    [RelayCommand]
    private async Task LoadAsync()
    {
        Status = "Yüklənir...";
        try
        {
            if (_catalog.Count == 0)
                _catalog = await api.GetPermissionCatalogAsync() ?? [];

            var current = SelectedRole?.Id;
            Roles.Clear();
            var roles = await api.GetRolesAsync();
            if (roles is not null) foreach (var r in roles) Roles.Add(r);

            SelectedRole = Roles.FirstOrDefault(r => r.Id == current) ?? Roles.FirstOrDefault();
            Status = $"{Roles.Count} rol";
        }
        catch (System.Exception ex) { Status = $"Xəta: {ex.Message}"; }
    }

    [RelayCommand]
    private async Task CreateRoleAsync()
    {
        if (string.IsNullOrWhiteSpace(NewRoleName)) { Status = "Rol adını yazın."; return; }
        var (ok, error) = await api.CreateRoleAsync(new CreateRoleRequest(NewRoleName!.Trim(), []));
        if (ok) { Status = $"Rol yaradıldı: {NewRoleName}. İndi icazələri seçin."; NewRoleName = null; await LoadAsync(); }
        else Status = error ?? "Yaradılmadı.";
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (SelectedRole is null) return;
        var chosen = Permissions.Where(p => p.IsChecked).Select(p => p.Key).ToList();
        var (ok, error) = await api.UpdateRolePermissionsAsync(SelectedRole.Id, new UpdateRolePermissionsRequest(chosen));
        if (ok) { Status = $"'{SelectedRole.Name}' icazələri yadda saxlandı ({chosen.Count})."; await LoadAsync(); }
        else Status = error ?? "Yadda saxlanmadı.";
    }

    [RelayCommand]
    private async Task DeleteRoleAsync()
    {
        if (SelectedRole is null) return;
        if (SelectedRole.IsSystem) { Status = "Daxili rol silinə bilməz."; return; }
        var (ok, error) = await api.DeleteRoleAsync(SelectedRole.Id);
        if (ok) { Status = "Rol silindi."; await LoadAsync(); }
        else Status = error ?? "Silinmədi.";
    }
}

/// <summary>Rol matrisində bir icazə qutusu.</summary>
public partial class PermissionCheck(string key, string label, bool isChecked) : ObservableObject
{
    public string Key { get; } = key;
    public string Label { get; } = label;
    [ObservableProperty] private bool _isChecked = isChecked;
}
