using ERP.Application.Common.Interfaces;
using ERP.Domain.Modules.Users;
using Microsoft.EntityFrameworkCore;

namespace ERP.Infrastructure.Persistence.Repositories;

/// <summary>Dinamik rollara xas repository implementasiyası (#16).</summary>
public sealed class RoleRepository(AppDbContext context)
    : Repository<AppRole>(context), IRoleRepository
{
    public async Task<AppRole?> GetByNameAsync(string name, CancellationToken ct = default)
    {
        var normalized = name.Trim().ToLower();
        return await Set.FirstOrDefaultAsync(r => r.Name.ToLower() == normalized, ct);
    }

    public async Task<IReadOnlyList<AppRole>> ListOrderedAsync(CancellationToken ct = default) =>
        await Set.AsNoTracking().OrderByDescending(r => r.IsSystem).ThenBy(r => r.Name).ToListAsync(ct);

    public async Task<bool> AnyAsync(CancellationToken ct = default) => await Set.AnyAsync(ct);
}
