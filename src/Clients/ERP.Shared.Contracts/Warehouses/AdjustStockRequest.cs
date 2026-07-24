namespace ERP.Shared.Contracts.Warehouses;

/// <summary>
/// Bir məhsulun bir anbardakı stokunu təyin etmək üçün request DTO-su (mütləq miqdar).
/// Səviyyə yoxdursa yaradılır, varsa yenilənir. MinQuantity minimum-stok həddidir.
/// </summary>
public sealed record AdjustStockRequest(
    Guid ProductId,
    Guid WarehouseId,
    int Quantity,
    int MinQuantity = 0);
