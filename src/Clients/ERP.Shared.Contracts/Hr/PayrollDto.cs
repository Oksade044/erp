namespace ERP.Shared.Contracts.Hr;

/// <summary>Əməkhaqqı hesablaması cavab DTO-su (TDD §12).</summary>
public sealed record PayrollDto(
    Guid Id,
    string PayrollNumber,
    Guid EmployeeId,
    string EmployeeName,
    int Year,
    int Month,
    decimal BaseSalary,
    decimal Bonus,
    decimal Deduction,
    decimal NetSalary,
    string Currency,
    string Status,
    DateOnly? PaidDate,
    string? Notes,
    DateTimeOffset CreatedAt);
