namespace ERP.Shared.Contracts.Hr;

/// <summary>
/// Yeni davamiyyət qeydi yaratmaq üçün request DTO-su.
/// Status: "Gəlib" | "Gəlməyib" | "Məzuniyyət" | "Xəstə" | "Yarımgün".
/// </summary>
public sealed record CreateAttendanceRequest(
    Guid EmployeeId,
    DateOnly Date,
    string Status,
    TimeOnly? CheckIn = null,
    TimeOnly? CheckOut = null,
    string? Notes = null);
