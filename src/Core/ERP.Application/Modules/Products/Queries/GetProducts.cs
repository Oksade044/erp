using ERP.Application.Common.Interfaces;
using ERP.Application.Common.Models;
using ERP.Shared.Contracts.Products;
using ERP.Application.Common.Messaging;

namespace ERP.Application.Modules.Products.Queries;

/// <summary>Məhsulları axtarış + səhifələmə ilə qaytarır (TDD §11, §17).</summary>
public sealed record GetProductsQuery(string? Search, int Page = 1, int PageSize = 20)
    : IRequest<PagedResult<ProductDto>>;

public sealed class GetProductsHandler(IProductRepository products)
    : IRequestHandler<GetProductsQuery, PagedResult<ProductDto>>
{
    public async Task<PagedResult<ProductDto>> Handle(GetProductsQuery request, CancellationToken ct)
    {
        var page = request.Page < 1 ? 1 : request.Page;
        var size = request.PageSize is < 1 or > 200 ? 20 : request.PageSize;

        var result = await products.SearchAsync(request.Search, page, size, ct);

        return new PagedResult<ProductDto>
        {
            Items = result.Items.Select(p => p.ToDto()).ToList(),
            TotalCount = result.TotalCount,
            Page = result.Page,
            PageSize = result.PageSize
        };
    }
}
