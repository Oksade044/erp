using ERP.Application.Common.Interfaces;
using ERP.Application.Common.Models;
using ERP.Domain.Modules.Products;
using Microsoft.EntityFrameworkCore;

namespace ERP.Infrastructure.Persistence.Repositories;

/// <summary>Məhsula xas repository implementasiyası (TDD §14).</summary>
public sealed class ProductRepository(AppDbContext context)
    : Repository<Product>(context), IProductRepository
{
    public async Task<bool> SkuExistsAsync(string normalizedSku, CancellationToken ct = default) =>
        await Set.AnyAsync(p => p.Sku.Value == normalizedSku, ct);

    public async Task<PagedResult<Product>> SearchAsync(
        string? search, int page, int pageSize, CancellationToken ct = default)
    {
        var query = Set.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(p =>
                p.Name.Contains(term) ||
                p.Sku.Value.Contains(term) ||
                (p.Category != null && p.Category.Contains(term)));
        }

        var total = await query.CountAsync(ct);

        var items = await query
            .OrderBy(p => p.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return new PagedResult<Product>
        {
            Items = items,
            TotalCount = total,
            Page = page,
            PageSize = pageSize
        };
    }
}
