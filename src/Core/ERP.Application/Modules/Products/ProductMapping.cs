using ERP.Domain.Exceptions;
using ERP.Domain.Modules.Products;
using ERP.Shared.Contracts.Products;

namespace ERP.Application.Modules.Products;

/// <summary>Product entity ↔ DTO çevirmələri (TDD §12).</summary>
public static class ProductMapping
{
    public static ProductDto ToDto(this Product p) => new(
        Id: p.Id,
        Sku: p.Sku.Value,
        Name: p.Name,
        Category: p.Category,
        Description: p.Description,
        RentalPrice: p.RentalPrice.Amount,
        PurchasePrice: p.PurchasePrice?.Amount,
        SalePrice: p.SalePrice?.Amount,
        Currency: p.RentalPrice.Currency,
        TrackingMode: p.TrackingMode.ToString(),
        StockQuantity: p.StockQuantity,
        MinStockQuantity: p.MinStockQuantity,
        IsLowStock: p.IsLowStock,
        IsActive: p.IsActive,
        HasImage: !string.IsNullOrWhiteSpace(p.ImagePath),
        CreatedAt: p.CreatedAt);

    public static ProductTrackingMode ParseTrackingMode(string? mode)
    {
        if (Enum.TryParse<ProductTrackingMode>(mode, ignoreCase: true, out var parsed))
            return parsed;
        throw new DomainException($"İzləmə rejimi düzgün deyil: {mode}. (Toplu | Nüsxə)");
    }
}
