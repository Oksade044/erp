using ERP.Application.Common.Interfaces;
using ERP.Application.Common.Models;
using ERP.Domain.Modules.Warehouses;
using Microsoft.EntityFrameworkCore;

namespace ERP.Infrastructure.Persistence.Repositories;

/// <summary>Anbara xas repository implementasiyası (TDD §14).</summary>
public sealed class WarehouseRepository(AppDbContext context)
    : Repository<Warehouse>(context), IWarehouseRepository
{
    public async Task<bool> CodeExistsAsync(string normalizedCode, CancellationToken ct = default) =>
        await Set.AnyAsync(w => w.Code == normalizedCode, ct);

    public async Task<PagedResult<Warehouse>> SearchAsync(
        string? search, int page, int pageSize, CancellationToken ct = default)
    {
        var query = Set.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(w => w.Name.Contains(term) || w.Code.Contains(term));
        }

        var total = await query.CountAsync(ct);

        var items = await query
            .OrderBy(w => w.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return new PagedResult<Warehouse>
        {
            Items = items,
            TotalCount = total,
            Page = page,
            PageSize = pageSize
        };
    }
}
