namespace ERP.Shared.Contracts.Hr;

/// <summary>Yeni işçi yaratmaq üçün request DTO-su.</summary>
public sealed record CreateEmployeeRequest(
    string FullName,
    string Position,
    string Phone,
    DateOnly HireDate,
    decimal Salary,
    string? Department = null,
    string? Email = null,
    string? Notes = null);
