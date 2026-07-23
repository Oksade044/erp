using ERP.Domain.Common;
using ERP.Domain.Exceptions;

namespace ERP.Domain.Modules.Hr;

/// <summary>
/// Davamiyyət qeydi — aggregate root, rich domain model (TDD §13). İşçinin bir gündəki iştirakı.
/// Bir işçi üçün bir gündə yalnız bir qeyd ola bilər (unikal: EmployeeId + Date, repository/DB-də).
/// Gəliş/gediş vaxtı verildikdə işlənmiş saatlar hesablanır.
/// </summary>
public class Attendance : BaseEntity, IAggregateRoot
{
    public Guid EmployeeId { get; private set; }

    /// <summary>İşçinin adı — snapshot (davamiyyət tarixçəsi üçün).</summary>
    public string EmployeeName { get; private set; } = null!;

    public DateOnly Date { get; private set; }
    public AttendanceStatus Status { get; private set; }

    public TimeOnly? CheckIn { get; private set; }
    public TimeOnly? CheckOut { get; private set; }

    public string? Notes { get; private set; }

    /// <summary>Gəliş və gediş vaxtı varsa işlənmiş saatlar; əks halda 0.</summary>
    public decimal WorkedHours =>
        CheckIn is { } inn && CheckOut is { } outt && outt > inn
            ? Math.Round((decimal)(outt - inn).TotalHours, 2)
            : 0m;

    // EF Core üçün.
    private Attendance() { }

    private Attendance(Guid employeeId, string employeeName, DateOnly date, AttendanceStatus status)
    {
        EmployeeId = employeeId;
        EmployeeName = employeeName;
        Date = date;
        Status = status;
    }

    public static Attendance Create(
        Guid employeeId,
        string employeeName,
        DateOnly date,
        AttendanceStatus status,
        TimeOnly? checkIn = null,
        TimeOnly? checkOut = null,
        string? notes = null)
    {
        if (employeeId == Guid.Empty)
            throw new DomainException("Davamiyyət üçün işçi tələb olunur.");
        if (string.IsNullOrWhiteSpace(employeeName))
            throw new DomainException("İşçi adı tələb olunur.");
        if (checkIn is { } i && checkOut is { } o && o < i)
            throw new DomainException("Gediş vaxtı gəliş vaxtından əvvəl ola bilməz.");

        return new Attendance(employeeId, employeeName.Trim(), date, status)
        {
            CheckIn = checkIn,
            CheckOut = checkOut,
            Notes = notes?.Trim()
        };
    }

    public void UpdateTimes(TimeOnly? checkIn, TimeOnly? checkOut)
    {
        if (checkIn is { } i && checkOut is { } o && o < i)
            throw new DomainException("Gediş vaxtı gəliş vaxtından əvvəl ola bilməz.");
        CheckIn = checkIn;
        CheckOut = checkOut;
    }

    public void ChangeStatus(AttendanceStatus status) => Status = status;
    public void SetNotes(string? notes) => Notes = notes?.Trim();
}
