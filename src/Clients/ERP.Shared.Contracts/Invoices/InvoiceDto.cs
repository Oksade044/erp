namespace ERP.Shared.Contracts.Invoices;

/// <summary>Faktura cavab DTO-su (ödənişlərlə). TDD §12.</summary>
public sealed record InvoiceDto(
    Guid Id,
    string InvoiceNumber,
    Guid OrderId,
    string OrderNumber,
    Guid CustomerId,
    string CustomerName,
    DateOnly IssueDate,
    decimal TotalAmount,
    decimal AmountPaid,
    decimal Balance,
    string Currency,
    string Status,
    IReadOnlyList<PaymentDto> Payments);

public sealed record PaymentDto(
    Guid Id,
    decimal Amount,
    string Currency,
    DateOnly PaidAt,
    string Method,
    string? Note);
