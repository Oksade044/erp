namespace ERP.Shared.Contracts.Products;

/// <summary>Mövcud məhsulu yeniləmək üçün request. SKU dəyişməz (avtomatik kod).</summary>
public sealed record UpdateProductRequest(
    string Name,
    decimal RentalPrice,
    string TrackingMode,
    decimal? PurchasePrice = null,
    decimal? SalePrice = null,
    string Currency = "AZN",
    int StockQuantity = 0,
    int MinStockQuantity = 0,
    string? Category = null,
    string? Description = null,
    bool IsActive = true,
    string Unit = "Ədəd");
