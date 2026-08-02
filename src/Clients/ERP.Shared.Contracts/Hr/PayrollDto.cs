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
    decimal PaidAmount,
    decimal Remaining,
    string Currency,
    string Status,
    DateOnly? PaidDate,
    string? Notes,
    DateTimeOffset CreatedAt,
    IReadOnlyList<PayrollPaymentDto> Payments);

/// <summary>Əməkhaqqı üzrə bir ödəniş (installment/bonus).</summary>
public sealed record PayrollPaymentDto(
    Guid Id,
    decimal Amount,
    string Currency,
    DateOnly Date,
    string Method,
    string? Note,
    bool IsBonus);
