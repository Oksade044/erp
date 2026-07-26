using ERP.Application.Common.Interfaces;
using ERP.Application.Common.Messaging;
using ERP.Shared.Contracts.Warehouses;

namespace ERP.Application.Modules.Warehouses.Queries;

/// <summary>Bir məhsulun bütün anbarlar üzrə stok səviyyələri (məhsul redaktəsində göstərmək üçün — #17).</summary>
public sealed record GetProductStockQuery(Guid ProductId) : IRequest<IReadOnlyList<StockLevelDto>>;

public sealed class GetProductStockHandler(IStockLevelRepository stockLevels)
    : IRequestHandler<GetProductStockQuery, IReadOnlyList<StockLevelDto>>
{
    public async Task<IReadOnlyList<StockLevelDto>> Handle(GetProductStockQuery request, CancellationToken ct)
    {
        var levels = await stockLevels.ListByProductsAsync([request.ProductId], ct);
        return levels
            .OrderBy(l => l.WarehouseName)
            .Select(l => l.ToDto())
            .ToList();
    }
}
