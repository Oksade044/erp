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
        var all = await Set.AsNoTracking().ToListAsync(ct);
        return RankedSearch.Page(all, search, page, pageSize,
            primary: w => w.Name,
            secondary: w => [w.Code]);
    }
}
