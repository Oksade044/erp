using ERP.Application.Common.Interfaces;
using ERP.Application.Common.Messaging;
using ERP.Application.Common.Models;
using ERP.Shared.Contracts.Warehouses;

namespace ERP.Application.Modules.Warehouses.Queries;

/// <summary>
/// Stok səviyyələrini axtarış + anbar filtri + yalnız-aşağı filtri ilə qaytarır (TDD §17, §11).
/// LowOnly=true minimum-stok xəbərdarlığı üçün.
/// </summary>
public sealed record GetStockLevelsQuery(string? Search, Guid? WarehouseId, bool LowOnly = false,
    int Page = 1, int PageSize = 20) : IRequest<PagedResult<StockLevelDto>>;

public sealed class GetStockLevelsHandler(IStockLevelRepository stockLevels)
    : IRequestHandler<GetStockLevelsQuery, PagedResult<StockLevelDto>>
{
    public async Task<PagedResult<StockLevelDto>> Handle(GetStockLevelsQuery request, CancellationToken ct)
    {
        var page = request.Page < 1 ? 1 : request.Page;
        var size = request.PageSize is < 1 or > 200 ? 20 : request.PageSize;

        var result = await stockLevels.SearchAsync(
            request.Search, request.WarehouseId, request.LowOnly, page, size, ct);

        return new PagedResult<StockLevelDto>
        {
            Items = result.Items.Select(s => s.ToDto()).ToList(),
            TotalCount = result.TotalCount,
            Page = result.Page,
            PageSize = result.PageSize
        };
    }
}
