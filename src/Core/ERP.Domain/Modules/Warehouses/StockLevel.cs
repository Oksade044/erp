using ERP.Domain.Common;
using ERP.Domain.Exceptions;

namespace ERP.Domain.Modules.Warehouses;

/// <summary>
/// Stok səviyyəsi — aggregate root (TDD §13). Bir məhsulun bir anbardakı miqdarı.
/// Çox-anbar (multi-warehouse) uçotunun əsasıdır; anbarlar arası transfer bu səviyyələri dəyişir.
/// Bir məhsul + anbar cütü üçün yalnız bir qeyd (unikal ProductId + WarehouseId).
/// MinQuantity minimum-stok həddidir; Quantity ondan aşağı düşəndə xəbərdarlıq (IsLow).
/// </summary>
public class StockLevel : BaseEntity, IAggregateRoot
{
    public Guid ProductId { get; private set; }
    public string ProductName { get; private set; } = null!;

    public Guid WarehouseId { get; private set; }
    public string WarehouseName { get; private set; } = null!;

    public int Quantity { get; private set; }
    public int MinQuantity { get; private set; }

    /// <summary>Miqdar minimum həddin altındadırsa xəbərdarlıq.</summary>
    public bool IsLow => Quantity < MinQuantity;

    // EF Core üçün.
    private StockLevel() { }

    private StockLevel(Guid productId, string productName, Guid warehouseId, string warehouseName)
    {
        ProductId = productId;
        ProductName = productName;
        WarehouseId = warehouseId;
        WarehouseName = warehouseName;
    }

    public static StockLevel Create(
        Guid productId, string productName, Guid warehouseId, string warehouseName,
        int quantity = 0, int minQuantity = 0)
    {
        if (productId == Guid.Empty)
            throw new DomainException("Məhsul tələb olunur.");
        if (warehouseId == Guid.Empty)
            throw new DomainException("Anbar tələb olunur.");
        if (quantity < 0)
            throw new DomainException("Miqdar mənfi ola bilməz.");
        if (minQuantity < 0)
            throw new DomainException("Minimum miqdar mənfi ola bilməz.");

        return new StockLevel(productId, productName.Trim(), warehouseId, warehouseName.Trim())
        {
            Quantity = quantity,
            MinQuantity = minQuantity
        };
    }

    public void Increase(int amount)
    {
        if (amount <= 0)
            throw new DomainException("Artım miqdarı 0-dan böyük olmalıdır.");
        Quantity += amount;
    }

    public void Decrease(int amount)
    {
        if (amount <= 0)
            throw new DomainException("Azalma miqdarı 0-dan böyük olmalıdır.");
        if (amount > Quantity)
            throw new DomainException(
                $"Anbarda kifayət qədər stok yoxdur ({WarehouseName}): mövcud {Quantity}, tələb {amount}.");
        Quantity -= amount;
    }

    public void SetQuantity(int quantity)
    {
        if (quantity < 0)
            throw new DomainException("Miqdar mənfi ola bilməz.");
        Quantity = quantity;
    }

    public void SetMinQuantity(int minQuantity)
    {
        if (minQuantity < 0)
            throw new DomainException("Minimum miqdar mənfi ola bilməz.");
        MinQuantity = minQuantity;
    }
}
