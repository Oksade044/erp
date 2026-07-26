namespace ERP.Shared.Contracts.Warehouses;

/// <summary>Stok səviyyəsi cavab DTO-su (TDD §12).</summary>
public sealed record StockLevelDto(
    Guid Id,
    Guid ProductId,
    string ProductName,
    Guid WarehouseId,
    string WarehouseName,
    int Quantity,
    int MinQuantity,
    bool IsLow,
    int InRepair = 0,
    int Damaged = 0);
