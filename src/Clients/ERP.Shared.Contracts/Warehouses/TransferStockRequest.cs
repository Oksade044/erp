namespace ERP.Shared.Contracts.Warehouses;

/// <summary>Anbarlar arası stok transferi üçün request DTO-su.</summary>
public sealed record TransferStockRequest(
    Guid ProductId,
    Guid FromWarehouseId,
    Guid ToWarehouseId,
    int Quantity);
