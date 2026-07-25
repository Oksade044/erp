using ERP.Application.Common.Interfaces;
using ERP.Application.Common.Models;
using ERP.Domain.Modules.Suppliers;
using Microsoft.EntityFrameworkCore;

namespace ERP.Infrastructure.Persistence.Repositories;

/// <summary>Təchizatçıya xas repository implementasiyası (TDD §14).</summary>
public sealed class SupplierRepository(AppDbContext context)
    : Repository<Supplier>(context), ISupplierRepository
{
    public async Task<bool> PhoneExistsAsync(string normalizedPhone, CancellationToken ct = default) =>
        await Set.AnyAsync(s => s.Phone.Value == normalizedPhone, ct);

    public async Task<PagedResult<Supplier>> SearchAsync(
        string? search, int page, int pageSize, CancellationToken ct = default)
    {
        var all = await Set.AsNoTracking().ToListAsync(ct);
        return RankedSearch.Page(all, search, page, pageSize,
            primary: s => s.Name,
            secondary: s => [s.Phone.Value, s.ContactPerson]);
    }
}
