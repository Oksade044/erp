using ERP.Domain.Common;
using ERP.Domain.Exceptions;
using ERP.Domain.ValueObjects;

namespace ERP.Domain.Modules.Hr;

/// <summary>
/// Əməkhaqqı üzrə bir ödəniş (hissə-hissə ödəniş — installment). Bir Payroll bir neçə
/// ödənişdən ibarət ola bilər (məs. 3000 maaşın 1500-ü bu ay, qalanı sonra). Hər ödənişdə
/// məbləğ, tarix, üsul və qeyd saxlanılır. Bonus da ayrıca ödəniş kimi qeyd oluna bilər.
/// </summary>
public class PayrollPayment : BaseEntity
{
    public Guid PayrollId { get; private set; }
    public Money Amount { get; private set; } = null!;
    public DateOnly Date { get; private set; }
    public string Method { get; private set; } = "Nağd";
    public string? Note { get; private set; }

    /// <summary>true — bu qeyd maaş deyil, ay üçün əlavə bonusdur.</summary>
    public bool IsBonus { get; private set; }

    // EF Core üçün.
    private PayrollPayment() { }

    internal PayrollPayment(Guid payrollId, Money amount, DateOnly date, string method, string? note, bool isBonus)
    {
        if (amount.Amount <= 0m)
            throw new DomainException("Ödəniş məbləği müsbət olmalıdır.");
        PayrollId = payrollId;
        Amount = amount;
        Date = date;
        Method = string.IsNullOrWhiteSpace(method) ? "Nağd" : method.Trim();
        Note = note?.Trim();
        IsBonus = isBonus;
    }
}
