using ERP.Application.Common.Interfaces;
using ERP.Application.Common.Models;
using ERP.Shared.Contracts.Products;
using ERP.Application.Common.Messaging;

namespace ERP.Application.Modules.Products.Queries;

/// <summary>Məhsulları axtarış + səhifələmə ilə qaytarır (TDD §11, §17).</summary>
public sealed record GetProductsQuery(string? Search, int Page = 1, int PageSize = 20)
    : IRequest<PagedResult<ProductDto>>;

public sealed class GetProductsHandler(
    IProductRepository products,
    IStockLevelRepository stockLevels,
    IAvailabilityReader availability)
    : IRequestHandler<GetProductsQuery, PagedResult<ProductDto>>
{
    public async Task<PagedResult<ProductDto>> Handle(GetProductsQuery request, CancellationToken ct)
    {
        var page = request.Page < 1 ? 1 : request.Page;
        var size = request.PageSize is < 1 or > 200 ? 20 : request.PageSize;

        var result = await products.SearchAsync(request.Search, page, size, ct);

        // Bu səhifədəki məhsulların anbar-stok xülasəsi + #27 yekun göstəriciləri (bir sorğuda).
        var ids = result.Items.Select(p => p.Id).ToList();
        var levels = await stockLevels.ListByProductsAsync(ids, ct);
        var summaryByProduct = levels
            .GroupBy(l => l.ProductId)
            .ToDictionary(
                g => g.Key,
                g => string.Join(", ", g.OrderBy(l => l.WarehouseName)
                    .Select(l => $"{l.WarehouseName}: {l.Quantity}")));
        var hasLevels = levels.Select(l => l.ProductId).ToHashSet();

        var summaries = await availability.GetSummariesAsync(ids, ct);

        return new PagedResult<ProductDto>
        {
            Items = result.Items.Select(p =>
            {
                var dto = p.ToDto();
                var sum = summaries.TryGetValue(p.Id, out var s) ? s : null;
                // #27 — ümumi stok anbarlardan; anbarda heç yoxdursa köhnə StockQuantity-yə düş.
                var total = hasLevels.Contains(p.Id) ? (sum?.Total ?? 0) : dto.StockQuantity;
                var free = hasLevels.Contains(p.Id)
                    ? (sum?.Free ?? 0)
                    : total - (sum?.Reserved ?? 0) - (sum?.Rented ?? 0);
                return dto with
                {
                    WarehouseSummary = summaryByProduct.TryGetValue(p.Id, out var ws) ? ws : null,
                    StockQuantity = total,
                    TotalStock = total,
                    FreeStock = free < 0 ? 0 : free,
                    ReservedStock = sum?.Reserved ?? 0,
                    RentedStock = sum?.Rented ?? 0,
                    InRepairStock = sum?.InRepair ?? 0,
                    DamagedStock = sum?.Damaged ?? 0
                };
            }).ToList(),
            TotalCount = result.TotalCount,
            Page = result.Page,
            PageSize = result.PageSize
        };
    }
}
