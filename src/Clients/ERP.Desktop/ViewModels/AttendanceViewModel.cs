using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ERP.Desktop.Services;
using ERP.Shared.Contracts.Hr;

namespace ERP.Desktop.ViewModels;

/// <summary>Davamiyyət ekranı — qeydlərin siyahısı, işçi seçimi ilə yeni gündəlik qeyd.</summary>
public partial class AttendanceViewModel(ErpApiClient api) : ViewModelBase
{
    public ObservableCollection<AttendanceDto> Records { get; } = [];
    public ObservableCollection<EmployeeDto> AllEmployees { get; } = [];

    [ObservableProperty] private string? _search;

    /// <summary>Canlı axtarış — yazıldıqca süzülür (Enter da işləyir).</summary>
    partial void OnSearchChanged(string? value) => DebounceReload(LoadAsync);
    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private string? _status;

    // Yeni qeyd forması
    [ObservableProperty] private EmployeeDto? _newEmployee;
    [ObservableProperty] private DateTimeOffset _newDate = DateTimeOffset.Now;
    [ObservableProperty] private string _newStatus = "Gəlib";
    [ObservableProperty] private string? _newCheckIn;
    [ObservableProperty] private string? _newCheckOut;
    [ObservableProperty] private string? _newNotes;

    public string[] AttendanceStatuses { get; } = ["Gəlib", "Gəlməyib", "Məzuniyyət", "Xəstə", "Yarımgün"];

    [RelayCommand]
    private async Task LoadAsync()
    {
        IsBusy = true;
        Status = "Yüklənir...";
        try
        {
            if (AllEmployees.Count == 0)
            {
                var emps = await api.GetEmployeesAsync(null);
                if (emps is not null) foreach (var e in emps.Items) AllEmployees.Add(e);
            }

            var result = await api.GetAttendanceAsync(Search);
            Records.Clear();
            if (result is not null)
                foreach (var r in result.Items) Records.Add(r);
            Status = $"{Records.Count} qeyd";
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
        if (NewEmployee is null) { Status = "İşçi seçin."; return; }

        IsBusy = true;
        try
        {
            var (ok, error) = await api.CreateAttendanceAsync(new CreateAttendanceRequest(
                EmployeeId: NewEmployee.Id,
                Date: DateOnly.FromDateTime(NewDate.DateTime),
                Status: NewStatus,
                CheckIn: TryParseTime(NewCheckIn),
                CheckOut: TryParseTime(NewCheckOut),
                Notes: NewNotes));

            if (ok)
            {
                Status = "Davamiyyət qeyd olundu.";
                NewCheckIn = NewCheckOut = NewNotes = null;
                await LoadAsync();
            }
            else Status = error ?? "Qeyd edilmədi.";
        }
        finally { IsBusy = false; }
    }

    /// <summary>"HH:mm" formatını TimeOnly-ə çevirir; boş/yanlış olduqda null.</summary>
    private static TimeOnly? TryParseTime(string? text) =>
        TimeOnly.TryParse(text, out var t) ? t : null;
}
