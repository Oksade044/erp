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
    IStockLevelRepository stockLevels)
    : IRequestHandler<GetProductsQuery, PagedResult<ProductDto>>
{
    public async Task<PagedResult<ProductDto>> Handle(GetProductsQuery request, CancellationToken ct)
    {
        var page = request.Page < 1 ? 1 : request.Page;
        var size = request.PageSize is < 1 or > 200 ? 20 : request.PageSize;

        var result = await products.SearchAsync(request.Search, page, size, ct);

        // Bu səhifədəki məhsulların anbar-stok xülasəsini bir sorğuda yığ (məhsul hansı anbarda).
        var ids = result.Items.Select(p => p.Id).ToList();
        var levels = await stockLevels.ListByProductsAsync(ids, ct);
        var summaryByProduct = levels
            .GroupBy(l => l.ProductId)
            .ToDictionary(
                g => g.Key,
                g => string.Join(", ", g.OrderBy(l => l.WarehouseName)
                    .Select(l => $"{l.WarehouseName}: {l.Quantity}")));

        return new PagedResult<ProductDto>
        {
            Items = result.Items.Select(p => p.ToDto() with
            {
                WarehouseSummary = summaryByProduct.TryGetValue(p.Id, out var s) ? s : null
            }).ToList(),
            TotalCount = result.TotalCount,
            Page = result.Page,
            PageSize = result.PageSize
        };
    }
}
