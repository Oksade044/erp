using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ERP.Desktop.Services;
using ERP.Shared.Contracts.Settings;

namespace ERP.Desktop.ViewModels;

/// <summary>
/// Sahə görünürlüyü — Admin/Menecer hansı rolun hansı həssas sahəni GÖRDÜyünü tənzimləyir.
/// Admin həmişə hər şeyi görür (dəyişdirilə bilməz).
/// </summary>
public partial class FieldPermissionsViewModel(ErpApiClient api) : ViewModelBase
{
    public ObservableCollection<FieldPermissionRow> Rows { get; } = [];

    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private string? _status;

    [RelayCommand]
    private async Task LoadAsync()
    {
        IsBusy = true;
        Status = "Yüklənir...";
        try
        {
            var perms = await api.GetFieldPermissionsAsync();
            Rows.Clear();
            if (perms is not null)
                foreach (var p in perms) Rows.Add(new FieldPermissionRow(p));
            Status = $"{Rows.Count} sahə. Dəyişiklikdən sonra 'Yadda saxla'.";
        }
        catch (System.Exception ex) { Status = $"Xəta: {ex.Message}"; }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        IsBusy = true;
        try
        {
            var ok = 0;
            foreach (var row in Rows)
            {
                var (success, error) = await api.UpdateFieldPermissionAsync(
                    new UpdateFieldPermissionRequest(row.FieldKey, row.SelectedRoles()));
                if (success) ok++;
                else { Status = error ?? "Yadda saxlanmadı."; return; }
            }
            Status = $"Yadda saxlandı ({ok} sahə). Dəyişiklik növbəti girişdə tam qüvvəyə minir.";
        }
        finally { IsBusy = false; }
    }
}

/// <summary>Bir həssas sahə sətri — rol qutuları (Admin həmişə seçili, kilidli).</summary>
public partial class FieldPermissionRow : ObservableObject
{
    public string FieldKey { get; }
    public string DisplayName { get; }

    [ObservableProperty] private bool _menecer;
    [ObservableProperty] private bool _anbardar;
    [ObservableProperty] private bool _kassir;

    public FieldPermissionRow(FieldPermissionDto dto)
    {
        FieldKey = dto.FieldKey;
        DisplayName = dto.DisplayName;
        Menecer = dto.AllowedRoles.Contains("Menecer");
        Anbardar = dto.AllowedRoles.Contains("Anbardar");
        Kassir = dto.AllowedRoles.Contains("Kassir");
    }

    /// <summary>Seçilmiş rollar (Admin həmişə daxil — server də bunu təmin edir).</summary>
    public List<string> SelectedRoles()
    {
        var roles = new List<string> { "Admin" };
        if (Menecer) roles.Add("Menecer");
        if (Anbardar) roles.Add("Anbardar");
        if (Kassir) roles.Add("Kassir");
        return roles;
    }
}
