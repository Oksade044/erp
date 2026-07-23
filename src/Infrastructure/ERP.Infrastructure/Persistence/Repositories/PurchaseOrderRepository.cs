using ERP.Application.Common.Interfaces;
using ERP.Application.Common.Models;
using ERP.Domain.Modules.Purchases;
using Microsoft.EntityFrameworkCore;

namespace ERP.Infrastructure.Persistence.Repositories;

/// <summary>Alış sifarişinə xas repository implementasiyası (TDD §14).</summary>
public sealed class PurchaseOrderRepository(AppDbContext context)
    : Repository<PurchaseOrder>(context), IPurchaseOrderRepository
{
    public async Task<PurchaseOrder?> GetByIdWithLinesAsync(Guid id, CancellationToken ct = default) =>
        await Set.Include(p => p.Lines).FirstOrDefaultAsync(p => p.Id == id, ct);

    public async Task<PagedResult<PurchaseOrder>> SearchAsync(
        string? search, int page, int pageSize, CancellationToken ct = default)
    {
        var query = Set.AsNoTracking().Include(p => p.Lines).AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(p =>
                p.PurchaseNumber.Contains(term) ||
                p.SupplierName.Contains(term));
        }

        var total = await query.CountAsync(ct);

        // SQLite DateTimeOffset-i ORDER BY-da dəstəkləmir; PurchaseNumber tarixlə başlayır
        // (ALS-yyyyMMdd-...) → hər iki provider-də sıralanır, ən yeni öndə.
        var items = await query
            .OrderByDescending(p => p.PurchaseNumber)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return new PagedResult<PurchaseOrder>
        {
            Items = items,
            TotalCount = total,
            Page = page,
            PageSize = pageSize
        };
    }
}
