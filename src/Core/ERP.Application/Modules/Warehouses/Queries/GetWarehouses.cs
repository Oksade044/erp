using ERP.Application.Common.Interfaces;
using ERP.Application.Common.Messaging;
using ERP.Application.Common.Models;
using ERP.Shared.Contracts.Warehouses;

namespace ERP.Application.Modules.Warehouses.Queries;

/// <summary>Anbarları axtarış + səhifələmə ilə qaytarır (TDD §17, §11).</summary>
public sealed record GetWarehousesQuery(string? Search, int Page = 1, int PageSize = 20)
    : IRequest<PagedResult<WarehouseDto>>;

public sealed class GetWarehousesHandler(IWarehouseRepository warehouses)
    : IRequestHandler<GetWarehousesQuery, PagedResult<WarehouseDto>>
{
    public async Task<PagedResult<WarehouseDto>> Handle(GetWarehousesQuery request, CancellationToken ct)
    {
        var page = request.Page < 1 ? 1 : request.Page;
        var size = request.PageSize is < 1 or > 200 ? 20 : request.PageSize;

        var result = await warehouses.SearchAsync(request.Search, page, size, ct);

        return new PagedResult<WarehouseDto>
        {
            Items = result.Items.Select(w => w.ToDto()).ToList(),
            TotalCount = result.TotalCount,
            Page = result.Page,
            PageSize = result.PageSize
        };
    }
}
