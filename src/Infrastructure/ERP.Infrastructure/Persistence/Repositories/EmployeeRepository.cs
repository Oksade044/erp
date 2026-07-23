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
        var query = Set.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(e =>
                e.FullName.Contains(term) ||
                e.Position.Contains(term) ||
                e.Phone.Value.Contains(term) ||
                (e.Department != null && e.Department.Contains(term)));
        }

        var total = await query.CountAsync(ct);

        var items = await query
            .OrderBy(e => e.FullName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return new PagedResult<Employee>
        {
            Items = items,
            TotalCount = total,
            Page = page,
            PageSize = pageSize
        };
    }
}
