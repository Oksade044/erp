using ERP.Application.Common.Interfaces;
using ERP.Domain.Modules.Products;
using Microsoft.EntityFrameworkCore;

namespace ERP.Infrastructure.Persistence.Repositories;

/// <summary>Kateqoriyalara xas repository implementasiyası (TDD §14).</summary>
public sealed class CategoryRepository(AppDbContext context)
    : Repository<Category>(context), ICategoryRepository
{
    public async Task<Category?> GetByNameAsync(string name, CancellationToken ct = default)
    {
        // Müqayisə C#-da (OrdinalIgnoreCase) aparılır — SQLite lower() Azərbaycan hərflərini
        // (Ç, Ş, İ, Ö, Ü, Ğ) düzgün kiçiltmir və bu, təkrar kateqoriya yaradılmasına səbəb olurdu.
        var normalized = name.Trim();
        var all = await Set.ToListAsync(ct);
        return all.FirstOrDefault(c => string.Equals(c.Name.Trim(), normalized, StringComparison.OrdinalIgnoreCase));
    }

    public async Task<IReadOnlyList<Category>> ListOrderedAsync(CancellationToken ct = default) =>
        await Set.AsNoTracking().OrderBy(c => c.Name).ToListAsync(ct);
}
