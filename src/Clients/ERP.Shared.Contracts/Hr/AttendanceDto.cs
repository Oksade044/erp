namespace ERP.Shared.Contracts.Hr;

/// <summary>Davamiyyət qeydi cavab DTO-su (TDD §12).</summary>
public sealed record AttendanceDto(
    Guid Id,
    Guid EmployeeId,
    string EmployeeName,
    DateOnly Date,
    string Status,
    TimeOnly? CheckIn,
    TimeOnly? CheckOut,
    decimal WorkedHours,
    string? Notes,
    DateTimeOffset CreatedAt);
