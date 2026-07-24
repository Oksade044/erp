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
            var term = search.Trim();
            query = query.Where(s => s.ProductName.Contains(term) || s.WarehouseName.Contains(term));
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
