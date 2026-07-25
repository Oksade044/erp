using ERP.Application.Common.Interfaces;
using ERP.Application.Common.Models;
using ERP.Domain.Modules.Hr;
using Microsoft.EntityFrameworkCore;

namespace ERP.Infrastructure.Persistence.Repositories;

/// <summary>İşçiyə xas repository implementasiyası (TDD §14).</summary>
public sealed class EmployeeRepository(AppDbContext context)
    : Repository<Employee>(context), IEmployeeRepository
{
    public async Task<bool> PhoneExistsAsync(string normalizedPhone, CancellationToken ct = default) =>
        await Set.AnyAsync(e => e.Phone.Value == normalizedPhone, ct);

    public async Task<PagedResult<Employee>> SearchAsync(
        string? search, int page, int pageSize, CancellationToken ct = default)
    {
        var all = await Set.AsNoTracking().ToListAsync(ct);
        return RankedSearch.Page(all, search, page, pageSize,
            primary: e => e.FullName,
            secondary: e => [e.Position, e.Phone.Value, e.Department]);
    }
}
