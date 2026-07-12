namespace ERP.Shared.Contracts.Products;

/// <summary>Mövcud məhsulu yeniləmək üçün request.</summary>
public sealed record UpdateProductRequest(
    string Name,
    decimal RentalPrice,
    string TrackingMode,
    string Currency = "AZN",
    int StockQuantity = 0,
    string? Category = null,
    string? Description = null,
    bool IsActive = true);
