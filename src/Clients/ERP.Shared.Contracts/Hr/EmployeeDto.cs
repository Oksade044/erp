namespace ERP.Shared.Contracts.Hr;

/// <summary>İşçi cavab DTO-su (TDD §12).</summary>
public sealed record EmployeeDto(
    Guid Id,
    string EmployeeNumber,
    string FullName,
    string Position,
    string? Department,
    string Phone,
    string? Email,
    DateOnly HireDate,
    decimal Salary,
    string Currency,
    string Status,
    string? Notes,
    DateTimeOffset CreatedAt);
