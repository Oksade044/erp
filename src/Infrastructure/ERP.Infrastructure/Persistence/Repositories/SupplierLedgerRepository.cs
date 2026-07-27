using ERP.Application.Common.Interfaces;
using ERP.Domain.Modules.Suppliers;
using Microsoft.EntityFrameworkCore;

namespace ERP.Infrastructure.Persistence.Repositories;

/// <summary>Təchizatçı defteri qeydlərinə xas repository (#15).</summary>
public sealed class SupplierLedgerRepository(AppDbContext context)
    : Repository<SupplierLedgerEntry>(context), ISupplierLedgerRepository
{
    public async Task<IReadOnlyList<SupplierLedgerEntry>> GetBySupplierAsync(
        Guid supplierId, CancellationToken ct = default)
    {
        // SQLite DateTimeOffset-i ORDER BY-da dəstəkləmir → CreatedAt üzrə sıralamanı klient tərəfdə et.
        var entries = await Set.AsNoTracking()
            .Where(e => e.SupplierId == supplierId)
            .ToListAsync(ct);

        return entries
            .OrderByDescending(e => e.Date)
            .ThenByDescending(e => e.CreatedAt)
            .ToList();
    }

    public async Task<SupplierLedgerEntry?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        await Set.FirstOrDefaultAsync(e => e.Id == id, ct);
}
