using ERP.Domain.Exceptions;
using ERP.Domain.Modules.Invoices;
using ERP.Shared.Contracts.Invoices;

namespace ERP.Application.Modules.Invoices;

/// <summary>Invoice entity → DTO çevirmələri (TDD §12).</summary>
public static class InvoiceMapping
{
    public static InvoiceDto ToDto(this Invoice i) => new(
        Id: i.Id,
        InvoiceNumber: i.InvoiceNumber,
        OrderId: i.OrderId,
        OrderNumber: i.OrderNumber,
        CustomerId: i.CustomerId,
        CustomerName: i.CustomerName,
        IssueDate: i.IssueDate,
        TotalAmount: i.TotalAmount.Amount,
        AmountPaid: i.AmountPaid.Amount,
        Balance: i.Balance.Amount,
        Currency: i.TotalAmount.Currency,
        Status: i.Status.ToString(),
        Payments: i.Payments.Select(p => new PaymentDto(
            Id: p.Id,
            Amount: p.Amount.Amount,
            Currency: p.Amount.Currency,
            PaidAt: p.PaidAt,
            Method: p.Method.ToString(),
            Note: p.Note)).ToList());

    public static PaymentMethod ParseMethod(string? method)
    {
        if (Enum.TryParse<PaymentMethod>(method, ignoreCase: true, out var parsed))
            return parsed;
        throw new DomainException($"Ödəniş üsulu düzgün deyil: {method}. (Nağd | Köçürmə | Kart)");
    }
}
