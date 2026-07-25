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
        await Set.IgnoreQueryFilters().AnyAsync(p => p.Sku.Value == normalizedSku, ct);

    public async Task<string> GenerateNextSkuAsync(CancellationToken ct = default)
    {
        const string prefix = "PRD-";

        // Silinmiş məhsulları da nəzərə al ki, SKU-lar təkrarlanmasın (unikallıq).
        var numbers = await Set.IgnoreQueryFilters()
            .Where(p => p.Sku.Value.StartsWith(prefix))
            .Select(p => p.Sku.Value)
            .ToListAsync(ct);

        var max = 0;
        foreach (var value in numbers)
            if (int.TryParse(value.AsSpan(prefix.Length), out var n) && n > max)
                max = n;

        return $"{prefix}{max + 1:000000}";
    }

    public async Task<PagedResult<Product>> SearchAsync(
        string? search, int page, int pageSize, CancellationToken ct = default)
    {
        // Diakritiksiz, sıralanmış axtarış (ortaq RankedSearch — SearchNormalizer əsaslı).
        var all = await Set.AsNoTracking().ToListAsync(ct);
        return RankedSearch.Page(all, search, page, pageSize,
            primary: p => p.Name,
            secondary: p => [p.Sku.Value, p.Category, p.Description]);
    }
}
