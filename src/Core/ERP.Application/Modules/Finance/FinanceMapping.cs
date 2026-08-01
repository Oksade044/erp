using ERP.Domain.Exceptions;
using ERP.Domain.Modules.Finance;
using ERP.Domain.Modules.Invoices;
using ERP.Shared.Contracts.Finance;

namespace ERP.Application.Modules.Finance;

/// <summary>FinancialTransaction entity ↔ DTO çevirmələri (TDD §12).</summary>
public static class FinanceMapping
{
    public static TransactionDto ToDto(this FinancialTransaction t) => new(
        Id: t.Id,
        TransactionNumber: t.TransactionNumber,
        Type: t.Type.ToString(),
        Category: t.Category,
        Amount: t.Amount.Amount,
        Currency: t.Amount.Currency,
        Date: t.Date,
        Method: t.Method.ToString(),
        Description: t.Description,
        CreatedAt: t.CreatedAt,
        PerformedBy: t.PerformedBy);

    public static TransactionType ParseType(string? type)
    {
        if (Enum.TryParse<TransactionType>(type, ignoreCase: true, out var parsed))
            return parsed;
        throw new DomainException($"Əməliyyat növü düzgün deyil: {type}. (Mədaxil | Məxaric)");
    }

    public static PaymentMethod ParseMethod(string? method)
    {
        if (Enum.TryParse<PaymentMethod>(method, ignoreCase: true, out var parsed))
            return parsed;
        throw new DomainException($"Ödəniş üsulu düzgün deyil: {method}. (Nağd | Köçürmə | Kart)");
    }
}
