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
        var normalized = name.Trim().ToLower();
        return await Set.FirstOrDefaultAsync(c => c.Name.ToLower() == normalized, ct);
    }

    public async Task<IReadOnlyList<Category>> ListOrderedAsync(CancellationToken ct = default) =>
        await Set.AsNoTracking().OrderBy(c => c.Name).ToListAsync(ct);
}
