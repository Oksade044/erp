namespace ERP.Shared.Contracts.Hr;

/// <summary>
/// Yeni əməkhaqqı hesablaması üçün request DTO-su. Baza maaş işçinin cari maaşından
/// avtomatik götürülür; bonus və tutulma opsionaldır.
/// </summary>
public sealed record CreatePayrollRequest(
    Guid EmployeeId,
    int Year,
    int Month,
    decimal Bonus = 0,
    decimal Deduction = 0,
    string? Notes = null);
