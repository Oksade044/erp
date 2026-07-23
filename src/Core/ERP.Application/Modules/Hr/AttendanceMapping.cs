using ERP.Domain.Exceptions;
using ERP.Domain.Modules.Hr;
using ERP.Shared.Contracts.Hr;

namespace ERP.Application.Modules.Hr;

/// <summary>Attendance entity ↔ DTO çevirmələri (TDD §12).</summary>
public static class AttendanceMapping
{
    public static AttendanceDto ToDto(this Attendance a) => new(
        Id: a.Id,
        EmployeeId: a.EmployeeId,
        EmployeeName: a.EmployeeName,
        Date: a.Date,
        Status: a.Status.ToString(),
        CheckIn: a.CheckIn,
        CheckOut: a.CheckOut,
        WorkedHours: a.WorkedHours,
        Notes: a.Notes,
        CreatedAt: a.CreatedAt);

    public static AttendanceStatus ParseStatus(string? status)
    {
        if (Enum.TryParse<AttendanceStatus>(status, ignoreCase: true, out var parsed))
            return parsed;
        throw new DomainException(
            $"Davamiyyət statusu düzgün deyil: {status}. (Gəlib | Gəlməyib | Məzuniyyət | Xəstə | Yarımgün)");
    }
}
