using ERP.Application.Common.Interfaces;
using ERP.Application.Common.Models;
using ERP.Domain.Modules.Warehouses;
using Microsoft.EntityFrameworkCore;

namespace ERP.Infrastructure.Persistence.Repositories;

/// <summary>Stok səviyyəsinə xas repository implementasiyası (TDD §14).</summary>
public sealed class StockLevelRepository(AppDbContext context)
    : Repository<StockLevel>(context), IStockLevelRepository
{
    public async Task<StockLevel?> GetAsync(Guid productId, Guid warehouseId, CancellationToken ct = default) =>
        await Set.FirstOrDefaultAsync(s => s.ProductId == productId && s.WarehouseId == warehouseId, ct);

    public async Task<IReadOnlyList<StockLevel>> ListByProductsAsync(IReadOnlyCollection<Guid> productIds, CancellationToken ct = default)
    {
        if (productIds.Count == 0) return [];
        return await Set.AsNoTracking()
            .Where(s => productIds.Contains(s.ProductId))
            .ToListAsync(ct);
    }

    public async Task<PagedResult<StockLevel>> SearchAsync(
        string? search, Guid? warehouseId, bool lowOnly, int page, int pageSize, CancellationToken ct = default)
    {
        var query = Set.AsNoTracking().AsQueryable();

        if (warehouseId is { } wid)
            query = query.Where(s => s.WarehouseId == wid);

        if (lowOnly)
            query = query.Where(s => s.Quantity < s.MinQuantity);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var all = await query.ToListAsync(ct);
            return RankedSearch.Page(all, search, page, pageSize,
                primary: s => s.ProductName,
                secondary: s => [s.WarehouseName]);
        }

        var total = await query.CountAsync(ct);

        var items = await query
            .OrderBy(s => s.ProductName)
            .ThenBy(s => s.WarehouseName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return new PagedResult<StockLevel>
        {
            Items = items,
            TotalCount = total,
            Page = page,
            PageSize = pageSize
        };
    }
}
