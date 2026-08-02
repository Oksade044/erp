using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ERP.Desktop.Services;
using ERP.Shared.Contracts.Hr;

namespace ERP.Desktop.ViewModels;

/// <summary>İşçilər (HR) ekranı — siyahı, axtarış və yeni işçi əlavəsi (API üzərindən).</summary>
public partial class EmployeesViewModel(ErpApiClient api, bool canViewSalary = true, bool createMode = false) : ViewModelBase
{
    /// <summary>#11 — yalnız təmsilçi yaratma ekranı (siyahı ayrı bölmədədir).</summary>
    public bool CreateMode { get; } = createMode;
    public bool ListMode => !createMode;

    /// <summary>Redaktə rejimi — siyahı bölməsində forma sahələrini göstərir.</summary>
    [ObservableProperty] private bool _isEditing;
    partial void OnIsEditingChanged(bool value)
    {
        OnPropertyChanged(nameof(FormVisible));
        OnPropertyChanged(nameof(SaveButtonText));
    }
    /// <summary>Forma sahələrinin görünürlüyü: yaratma bölməsi VƏ YA redaktə.</summary>
    public bool FormVisible => CreateMode || IsEditing;
    public string SaveButtonText => IsEditing ? "✓ Yadda saxla" : "+ Əlavə et";

    public ObservableCollection<EmployeeDto> Employees { get; } = [];
    [ObservableProperty] private EmployeeDto? _selected;
    private Guid? _editId;
    private string _editStatus = "İşləyir";

    /// <summary>Redaktədən çıx (dəyişiklikləri saxlamadan).</summary>
    [RelayCommand]
    private void CancelEdit()
    {
        _editId = null; IsEditing = false;
        NewFullName = NewPosition = NewDepartment = NewPhone = NewEmail = null; NewSalary = 0;
    }

    /// <summary>Maaş sütunu/sahəsinin görünürlüyü — sahə icazəsindən (employee.salary).</summary>
    public bool CanViewSalary { get; } = canViewSalary;

    /// <summary>Seçilmiş təmsilçini silir (soft delete).</summary>
    [RelayCommand]
    private async Task DeleteSelectedAsync()
    {
        if (Selected is null) { ERP.Desktop.AppNotify.Show("Silmək üçün təmsilçi seçin."); return; }
        var name = Selected.FullName;
        var (ok, err) = await api.DeleteEmployeeAsync(Selected.Id);
        ERP.Desktop.AppNotify.Show(ok ? $"Təmsilçi silindi: {name}" : err ?? "Silinmədi.");
        if (ok) await LoadAsync();
    }

    /// <summary>Seçilmiş təmsilçini forma sahələrinə yükləyir — 'Yadda saxla' yeniləyir.</summary>
    [RelayCommand]
    private void EditSelected()
    {
        if (Selected is null) { ERP.Desktop.AppNotify.Show("Redaktə üçün təmsilçi seçin."); return; }
        _editId = Selected.Id; _editStatus = Selected.Status;
        NewFullName = Selected.FullName; NewPosition = Selected.Position; NewDepartment = Selected.Department;
        NewPhone = Selected.Phone; NewEmail = Selected.Email; NewSalary = Selected.Salary;
        NewHireDate = new DateTimeOffset(Selected.HireDate.ToDateTime(TimeOnly.MinValue));
        IsEditing = true;
        ERP.Desktop.AppNotify.Show("Məlumatı dəyişib 'Yadda saxla' basın.");
    }

    [ObservableProperty] private string? _search;

    /// <summary>Canlı axtarış — yazıldıqca süzülür (Enter da işləyir).</summary>
    partial void OnSearchChanged(string? value) => DebounceReload(LoadAsync);
    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private string? _status;

    // Yeni işçi forması
    [ObservableProperty] private string? _newFullName;
    [ObservableProperty] private string? _newPosition;
    [ObservableProperty] private string? _newDepartment;
    [ObservableProperty] private string? _newPhone;
    [ObservableProperty] private string? _newEmail;
    [ObservableProperty] private DateTimeOffset _newHireDate = DateTimeOffset.Now;
    [ObservableProperty] private decimal _newSalary;

    [RelayCommand]
    private async Task LoadAsync()
    {
        IsBusy = true;
        Status = "Yüklənir...";
        try
        {
            var result = await api.GetEmployeesAsync(Search);
            Employees.Clear();
            if (result is not null)
                foreach (var e in result.Items) Employees.Add(e);
            Status = $"{Employees.Count} işçi";
        }
        catch (System.Exception ex)
        {
            Status = $"Xəta: {ex.Message}";
        }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private async Task AddAsync()
    {
        if (string.IsNullOrWhiteSpace(NewFullName) || string.IsNullOrWhiteSpace(NewPosition)
            || string.IsNullOrWhiteSpace(NewPhone))
        {
            Status = "Ad, vəzifə və telefon tələb olunur.";
            return;
        }

        IsBusy = true;
        try
        {
            bool ok; string? error;
            if (_editId is { } id)
                (ok, error) = await api.UpdateEmployeeAsync(id, new UpdateEmployeeRequest(
                    FullName: NewFullName!, Position: NewPosition!, Phone: NewPhone!,
                    Salary: NewSalary, Status: _editStatus, Department: NewDepartment, Email: NewEmail));
            else
                (ok, error) = await api.CreateEmployeeAsync(new CreateEmployeeRequest(
                    FullName: NewFullName!,
                    Position: NewPosition!,
                    Phone: NewPhone!,
                    HireDate: DateOnly.FromDateTime(NewHireDate.DateTime),
                    Salary: NewSalary,
                    Department: NewDepartment,
                    Email: NewEmail));

            if (ok)
            {
                Status = _editId is null ? "İşçi əlavə olundu." : "İşçi yeniləndi.";
                ERP.Desktop.AppNotify.Show(Status);
                _editId = null;
                IsEditing = false;
                NewFullName = NewPosition = NewDepartment = NewPhone = NewEmail = null;
                NewSalary = 0;
                await LoadAsync();
            }
            else Status = error ?? "Əlavə edilmədi.";
        }
        finally { IsBusy = false; }
    }
}
