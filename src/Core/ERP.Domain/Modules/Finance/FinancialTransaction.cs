using ERP.Domain.Common;
using ERP.Domain.Exceptions;
using ERP.Domain.Modules.Invoices;
using ERP.Domain.ValueObjects;

namespace ERP.Domain.Modules.Finance;

/// <summary>
/// Maliyyə əməliyyatı (kassa mədaxil/məxaric) — aggregate root, rich domain model (TDD §13).
/// Gəlir və xərclərin vahid registridir; kassa balansı bu əməliyyatların cəmidir.
/// Məbləğ həmişə müsbət Money-dir; istiqamət Type ilə müəyyən olunur (işarəli məbləğ SignedAmount).
/// </summary>
public class FinancialTransaction : BaseEntity, IAggregateRoot
{
    public string TransactionNumber { get; private set; } = null!;
    public TransactionType Type { get; private set; }

    /// <summary>Kateqoriya (məs. "İcarə gəliri", "Əməkhaqqı", "Kirayə", "Kommunal").</summary>
    public string Category { get; private set; } = null!;

    public Money Amount { get; private set; } = null!;
    public DateOnly Date { get; private set; }
    public PaymentMethod Method { get; private set; }
    public string? Description { get; private set; }

    /// <summary>Əməliyyatı edən (#4 Kassa): "Mərkəz" və ya təmsilçi adı.</summary>
    public string? PerformedBy { get; private set; }

    /// <summary>Mədaxil üçün +, məxaric üçün − işarəli məbləğ (balans hesablaması üçün).</summary>
    public decimal SignedAmount => Type == TransactionType.Mədaxil ? Amount.Amount : -Amount.Amount;

    // EF Core üçün.
    private FinancialTransaction() { }

    private FinancialTransaction(
        string number, TransactionType type, string category, Money amount, DateOnly date, PaymentMethod method)
    {
        TransactionNumber = number;
        Type = type;
        Category = category;
        Amount = amount;
        Date = date;
        Method = method;
    }

    public static FinancialTransaction Create(
        string number,
        TransactionType type,
        string category,
        Money amount,
        DateOnly date,
        PaymentMethod method,
        string? description = null,
        string? performedBy = null)
    {
        if (string.IsNullOrWhiteSpace(number))
            throw new DomainException("Əməliyyat nömrəsi tələb olunur.");
        if (string.IsNullOrWhiteSpace(category))
            throw new DomainException("Kateqoriya tələb olunur.");
        if (amount.Amount <= 0)
            throw new DomainException("Məbləğ 0-dan böyük olmalıdır.");

        return new FinancialTransaction(number, type, category.Trim(), amount, date, method)
        {
            Description = description?.Trim(),
            PerformedBy = string.IsNullOrWhiteSpace(performedBy) ? "Mərkəz" : performedBy.Trim()
        };
    }

    public void UpdateDescription(string? description) => Description = description?.Trim();
}
