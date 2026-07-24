namespace ERP.Shared.Contracts.Products;

/// <summary>Məhsul cavab DTO-su (TDD §12). Domenə asılılıq yoxdur.</summary>
public sealed record ProductDto(
    Guid Id,
    string Sku,
    string Name,
    string? Category,
    string? Description,
    decimal RentalPrice,
    string Currency,
    string TrackingMode,
    int StockQuantity,
    bool IsActive,
    bool HasImage,
    DateTimeOffset CreatedAt);
