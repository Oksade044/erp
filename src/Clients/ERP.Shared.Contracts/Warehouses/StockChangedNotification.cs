namespace ERP.Shared.Contracts.Warehouses;

/// <summary>
/// Canlı stok dəyişikliyi bildirişi (SignalR ilə yayımlanır, TDD §38). Bir istifadəçi stoku
/// dəyişəndə (təyin/transfer) digərləri dərhal görür — ikiqat-bronun və köhnə datanın qarşısı.
/// </summary>
public sealed record StockChangedNotification(
    Guid ProductId,
    string ProductName,
    Guid WarehouseId,
    string WarehouseName,
    int Quantity,
    int MinQuantity,
    bool IsLow);
