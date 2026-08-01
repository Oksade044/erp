using ERP.Application.Common.Interfaces;
using ERP.Domain.Modules.Representatives;
using Microsoft.EntityFrameworkCore;

namespace ERP.Infrastructure.Persistence.Repositories;

/// <summary>Təmsilçi defterinə xas repository (#16-18).</summary>
public sealed class RepresentativeRepository(AppDbContext context)
    : Repository<RepresentativeEntry>(context), IRepresentativeRepository
{
    public async Task<IReadOnlyList<RepresentativeEntry>> GetByRepresentativeAsync(string name, CancellationToken ct = default)
    {
        // SQLite DateTimeOffset-i ORDER BY-da dəstəkləmir → sıralamanı klient tərəfdə et.
        var entries = await Set.AsNoTracking()
            .Where(e => e.RepresentativeName == name)
            .ToListAsync(ct);
        return entries.OrderByDescending(e => e.Date).ThenByDescending(e => e.CreatedAt).ToList();
    }

    public async Task<IReadOnlyList<RepresentativeEntry>> GetAllAsync(CancellationToken ct = default) =>
        await Set.AsNoTracking().ToListAsync(ct);
}
