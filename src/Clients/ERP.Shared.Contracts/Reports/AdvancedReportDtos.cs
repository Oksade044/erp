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

/// <summary>Zədə/itki hesabatı sətri — hesablaşması aparılmış, tutulması olan sifarişlər.</summary>
public sealed record DamageReportRowDto(
    string OrderNumber,
    string CustomerName,
    decimal DamageCharge,
    decimal PenaltyCharge,
    decimal TotalCharges,
    string Currency,
    string? SettlementNotes);
