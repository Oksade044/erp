namespace ERP.Shared.Contracts.Products;

/// <summary>Yeni məhsul yaratmaq üçün request. TrackingMode: "Toplu" | "Nüsxə".</summary>
public sealed record CreateProductRequest(
    string Sku,
    string Name,
    decimal RentalPrice,
    string TrackingMode,
    string Currency = "AZN",
    int StockQuantity = 0,
    string? Category = null,
    string? Description = null);
