using ERP.Domain.Common;
using ERP.Domain.Exceptions;
using ERP.Domain.ValueObjects;

namespace ERP.Domain.Modules.Purchases;

/// <summary>
/// Alış sətri — PurchaseOrder aggregate-inin daxili hissəsi (ayrıca aggregate root DEYİL).
/// Yalnız PurchaseOrder vasitəsilə dəyişdirilir. Alış qiyməti (UnitCost) sifariş anında
/// snapshot kimi saxlanılır.
/// </summary>
public class PurchaseLine : BaseEntity
{
    public Guid PurchaseId { get; private set; }
    public Guid ProductId { get; private set; }

    /// <summary>Məhsul adı — snapshot (alış tarixçəsi üçün).</summary>
    public string ProductName { get; private set; } = null!;

    public int Quantity { get; private set; }

    /// <summary>Bir vahidin alış (maya) qiyməti.</summary>
    public Money UnitCost { get; private set; } = null!;

    public Money LineTotal => UnitCost.Multiply(Quantity);

    // EF Core üçün.
    private PurchaseLine() { }

    internal PurchaseLine(Guid productId, string productName, int quantity, Money unitCost)
    {
        if (productId == Guid.Empty)
            throw new DomainException("Sətir üçün məhsul tələb olunur.");
        if (string.IsNullOrWhiteSpace(productName))
            throw new DomainException("Məhsul adı tələb olunur.");
        if (quantity <= 0)
            throw new DomainException("Say 0-dan böyük olmalıdır.");

        ProductId = productId;
        ProductName = productName.Trim();
        Quantity = quantity;
        UnitCost = unitCost;
    }

    internal void ChangeQuantity(int quantity)
    {
        if (quantity <= 0)
            throw new DomainException("Say 0-dan böyük olmalıdır.");
        Quantity = quantity;
    }
}
