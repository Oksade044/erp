namespace ERP.Shared.Contracts.Products;

/// <summary>
/// Yeni məhsul yaratmaq üçün request. TrackingMode: "Toplu" | "Nüsxə".
/// Sku boş verilə bilər — server avtomatik PRD-000001 formatında generasiya edir.
/// </summary>
public sealed record CreateProductRequest(
    string Name,
    decimal RentalPrice,
    string TrackingMode,
    string? Sku = null,
    decimal? PurchasePrice = null,
    decimal? SalePrice = null,
    string Currency = "AZN",
    int StockQuantity = 0,
    int MinStockQuantity = 0,
    string? Category = null,
    string? Description = null,
    // Köhnə: tək anbar (geriyə uyğunluq üçün saxlanılır).
    Guid? WarehouseId = null,
    // Yeni: məhsul bir neçə anbarda müxtəlif sayda ola bilər — hər anbar üçün ilkin stok.
    IReadOnlyList<InitialStockRequest>? InitialStocks = null);

/// <summary>Yeni məhsulun bir anbardakı ilkin stoku.</summary>
public sealed record InitialStockRequest(
    Guid WarehouseId,
    int Quantity,
    int MinQuantity = 0);
