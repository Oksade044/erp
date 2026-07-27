namespace ERP.Shared.Contracts.Reports;

/// <summary>Müştəri hesabatı sətri — müştəri üzrə sifariş sayı və maliyyə xülasəsi.</summary>
public sealed record CustomerReportRowDto(
    Guid CustomerId,
    string CustomerName,
    int OrderCount,
    decimal TotalInvoiced,
    decimal TotalPaid,
    decimal Outstanding,
    string Currency);

/// <summary>İşçi performansı — əməkdaş üzrə sifariş sayı və ümumi dövriyyə (#24).</summary>
public sealed record EmployeePerformanceRowDto(
    string EmployeeName,
    string? Role,
    int OrderCount,
    decimal TotalRevenue,
    string Currency);

/// <summary>
/// İcarə təqvimi sətri — verilmiş dövrlə kəsişən icarə sifarişləri (planlaşdırma üçün).
/// DeliversInRange/ReturnsInRange dövr daxilində təhvil/qaytarma olub-olmadığını göstərir.
/// </summary>
public sealed record RentalCalendarEntryDto(
    Guid OrderId,
    string OrderNumber,
    string CustomerName,
    DateOnly StartDate,
    DateOnly EndDate,
    string Status,
    decimal Total,
    string Currency,
    bool DeliversInRange,
    bool ReturnsInRange);

/// <summary>Zədə/itki hesabatı sətri — hesablaşması aparılmış, tutulması olan sifarişlər.</summary>
public sealed record DamageReportRowDto(
    string OrderNumber,
    string CustomerName,
    decimal DamageCharge,
    decimal PenaltyCharge,
    decimal TotalCharges,
    string Currency,
    string? SettlementNotes);
