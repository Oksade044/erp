using ERP.Domain.Common;
using ERP.Domain.Exceptions;
using ERP.Domain.ValueObjects;

namespace ERP.Domain.Modules.Invoices;

/// <summary>
/// Ödəniş — Invoice aggregate-inin daxili hissəsi (ayrıca aggregate root deyil).
/// Yalnız Invoice vasitəsilə əlavə olunur.
/// </summary>
public class Payment : BaseEntity
{
    public Guid InvoiceId { get; private set; }
    public Money Amount { get; private set; } = null!;
    public DateOnly PaidAt { get; private set; }
    public PaymentMethod Method { get; private set; }
    public string? Note { get; private set; }

    // EF Core üçün.
    private Payment() { }

    internal Payment(Money amount, DateOnly paidAt, PaymentMethod method, string? note)
    {
        if (amount.Amount <= 0)
            throw new DomainException("Ödəniş məbləği 0-dan böyük olmalıdır.");

        Amount = amount;
        PaidAt = paidAt;
        Method = method;
        Note = note?.Trim();
    }
}
