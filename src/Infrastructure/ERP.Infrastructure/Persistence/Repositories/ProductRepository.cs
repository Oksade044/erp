using ERP.Application.Common.Interfaces;
using ERP.Application.Common.Models;
using ERP.Domain.Common;
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
        // Axtarış boşdursa — sadə DB-səviyyəli səhifələmə (böyük kataloqda da səmərəli).
        if (string.IsNullOrWhiteSpace(search))
        {
            var baseQuery = Set.AsNoTracking();
            var totalAll = await baseQuery.CountAsync(ct);
            var pageItems = await baseQuery
                .OrderBy(p => p.Name)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(ct);

            return new PagedResult<Product>
            {
                Items = pageItems, TotalCount = totalAll, Page = page, PageSize = pageSize
            };
        }

        // Diakritiksiz, sıralanmış axtarış. SQLite Azərbaycan hərflərini/diakritiki SQL-də
        // düzgün süzə bilmədiyi üçün normalizasiya + sıralama yaddaşda aparılır (kataloq lokaldır).
        var term = SearchNormalizer.Normalize(search);

        var all = await Set.AsNoTracking().ToListAsync(ct);

        var ranked = all
            .Select(p => (product: p, rank: SearchNormalizer.Score(
                term, p.Name, [p.Sku.Value, p.Category, p.Description])))
            .Where(x => x.rank != SearchNormalizer.NoMatch)
            .OrderBy(x => x.rank)             // tam uyğun → söz əvvəli → daxil
            .ThenBy(x => x.product.Name.Length)
            .ThenBy(x => x.product.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var items = ranked
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => x.product)
            .ToList();

        return new PagedResult<Product>
        {
            Items = items, TotalCount = ranked.Count, Page = page, PageSize = pageSize
        };
    }
}
