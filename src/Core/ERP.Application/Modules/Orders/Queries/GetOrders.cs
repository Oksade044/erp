using ERP.Application.Common.Interfaces;
using ERP.Application.Common.Models;
using ERP.Shared.Contracts.Orders;
using ERP.Application.Common.Messaging;

namespace ERP.Application.Modules.Orders.Queries;

/// <summary>Sifarişləri axtarış + səhifələmə ilə qaytarır (TDD §11, §17).</summary>
public sealed record GetOrdersQuery(string? Search, int Page = 1, int PageSize = 20)
    : IRequest<PagedResult<OrderDto>>;

public sealed class GetOrdersHandler(IRentalOrderRepository orders)
    : IRequestHandler<GetOrdersQuery, PagedResult<OrderDto>>
{
    public async Task<PagedResult<OrderDto>> Handle(GetOrdersQuery request, CancellationToken ct)
    {
        var page = request.Page < 1 ? 1 : request.Page;
        var size = request.PageSize is < 1 or > 200 ? 20 : request.PageSize;

        var result = await orders.SearchAsync(request.Search, page, size, ct);

        return new PagedResult<OrderDto>
        {
            Items = result.Items.Select(o => o.ToDto()).ToList(),
            TotalCount = result.TotalCount,
            Page = result.Page,
            PageSize = result.PageSize
        };
    }
}
