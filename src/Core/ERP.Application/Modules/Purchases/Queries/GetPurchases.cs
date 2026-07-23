using ERP.Application.Common.Interfaces;
using ERP.Application.Common.Messaging;
using ERP.Application.Common.Models;
using ERP.Shared.Contracts.Purchases;

namespace ERP.Application.Modules.Purchases.Queries;

/// <summary>Alışları axtarış + səhifələmə ilə qaytarır (TDD §17, §11).</summary>
public sealed record GetPurchasesQuery(string? Search, int Page = 1, int PageSize = 20)
    : IRequest<PagedResult<PurchaseDto>>;

public sealed class GetPurchasesHandler(IPurchaseOrderRepository purchases)
    : IRequestHandler<GetPurchasesQuery, PagedResult<PurchaseDto>>
{
    public async Task<PagedResult<PurchaseDto>> Handle(GetPurchasesQuery request, CancellationToken ct)
    {
        var page = request.Page < 1 ? 1 : request.Page;
        var size = request.PageSize is < 1 or > 200 ? 20 : request.PageSize;

        var result = await purchases.SearchAsync(request.Search, page, size, ct);

        return new PagedResult<PurchaseDto>
        {
            Items = result.Items.Select(p => p.ToDto()).ToList(),
            TotalCount = result.TotalCount,
            Page = result.Page,
            PageSize = result.PageSize
        };
    }
}
