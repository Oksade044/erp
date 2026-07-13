namespace ERP.Shared.Contracts.Invoices;

/// <summary>Mövcud sifarişdən faktura yaratmaq üçün request.</summary>
public sealed record CreateInvoiceRequest(Guid OrderId);

/// <summary>Fakturaya ödəniş əlavə etmək üçün request. Method: "Nağd" | "Köçürmə" | "Kart".</summary>
public sealed record AddPaymentRequest(
    decimal Amount,
    string Method,
    DateOnly? PaidAt = null,
    string? Note = null);
