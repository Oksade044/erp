namespace ERP.Shared.Contracts.Products;

/// <summary>Məhsul cavab DTO-su (TDD §12). Domenə asılılıq yoxdur.</summary>
public sealed record ProductDto(
    Guid Id,
    string Sku,
    string Name,
    string? Category,
    string? Description,
    decimal RentalPrice,
    decimal? PurchasePrice,
    decimal? SalePrice,
    string Currency,
    string TrackingMode,
    int StockQuantity,
    int MinStockQuantity,
    bool IsLowStock,
    bool IsActive,
    bool HasImage,
    DateTimeOffset CreatedAt,
    // Məhsulun anbarlar üzrə stoku (məs. "Mərkəzi anbar: 50, Filial: 10"). Siyahıda boş ola bilər.
    string? WarehouseSummary = null);
