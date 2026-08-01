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

    public ObservableCollection<EmployeeDto> Employees { get; } = [];

    /// <summary>Maaş sütunu/sahəsinin görünürlüyü — sahə icazəsindən (employee.salary).</summary>
    public bool CanViewSalary { get; } = canViewSalary;

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
            var (ok, error) = await api.CreateEmployeeAsync(new CreateEmployeeRequest(
                FullName: NewFullName!,
                Position: NewPosition!,
                Phone: NewPhone!,
                HireDate: DateOnly.FromDateTime(NewHireDate.DateTime),
                Salary: NewSalary,
                Department: NewDepartment,
                Email: NewEmail));

            if (ok)
            {
                Status = "İşçi əlavə olundu.";
                NewFullName = NewPosition = NewDepartment = NewPhone = NewEmail = null;
                NewSalary = 0;
                await LoadAsync();
            }
            else Status = error ?? "Əlavə edilmədi.";
        }
        finally { IsBusy = false; }
    }
}
