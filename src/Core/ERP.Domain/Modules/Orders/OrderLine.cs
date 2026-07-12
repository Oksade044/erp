using ERP.Domain.Common;
using ERP.Domain.Exceptions;
using ERP.Domain.ValueObjects;

namespace ERP.Domain.Modules.Orders;

/// <summary>
/// Sifariş sətri — RentalOrder aggregate-inin daxili hissəsi (ayrıca aggregate root DEYİL).
/// Yalnız RentalOrder vasitəsilə dəyişdirilir. Qiymət sifariş anında snapshot kimi saxlanılır
/// (məhsulun baza qiyməti sonradan dəyişsə belə, sifariş qiyməti sabit qalır — TDD §7 dinamik qiymət).
/// </summary>
public class OrderLine : BaseEntity
{
    public Guid OrderId { get; private set; }
    public Guid ProductId { get; private set; }

    /// <summary>Məhsul adı — snapshot (sifariş tarixçəsi üçün).</summary>
    public string ProductName { get; private set; } = null!;

    public int Quantity { get; private set; }

    /// <summary>Bir vahid üçün icarə qiyməti (sifarişə xas dinamik dəyər).</summary>
    public Money UnitPrice { get; private set; } = null!;

    public Money LineTotal => UnitPrice.Multiply(Quantity);

    // EF Core üçün.
    private OrderLine() { }

    internal OrderLine(Guid productId, string productName, int quantity, Money unitPrice)
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
        UnitPrice = unitPrice;
    }

    internal void ChangeQuantity(int quantity)
    {
        if (quantity <= 0)
            throw new DomainException("Say 0-dan böyük olmalıdır.");
        Quantity = quantity;
    }
}
