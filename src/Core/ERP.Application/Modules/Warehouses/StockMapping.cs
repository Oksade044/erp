using ERP.Domain.Modules.Warehouses;
using ERP.Shared.Contracts.Warehouses;

namespace ERP.Application.Modules.Warehouses;

/// <summary>StockLevel entity → DTO çevirmələri (TDD §12).</summary>
public static class StockMapping
{
    public static StockLevelDto ToDto(this StockLevel s) => new(
        Id: s.Id,
        ProductId: s.ProductId,
        ProductName: s.ProductName,
        WarehouseId: s.WarehouseId,
        WarehouseName: s.WarehouseName,
        Quantity: s.Quantity,
        MinQuantity: s.MinQuantity,
        IsLow: s.IsLow);

    /// <summary>Canlı SignalR bildirişi üçün (TDD §38).</summary>
    public static StockChangedNotification ToNotification(this StockLevel s) => new(
        ProductId: s.ProductId,
        ProductName: s.ProductName,
        WarehouseId: s.WarehouseId,
        WarehouseName: s.WarehouseName,
        Quantity: s.Quantity,
        MinQuantity: s.MinQuantity,
        IsLow: s.IsLow);
}
