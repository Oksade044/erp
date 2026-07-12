using ERP.Application.Common.Interfaces;
using ERP.Application.Common.Models;
using ERP.Domain.Modules.Customers;
using Microsoft.EntityFrameworkCore;

namespace ERP.Infrastructure.Persistence.Repositories;

/// <summary>
/// Müştəriyə xas repository implementasiyası (TDD §14). SQL/EF Core burada qalır.
/// </summary>
public sealed class CustomerRepository(AppDbContext context)
    : Repository<Customer>(context), ICustomerRepository
{
    public async Task<Customer?> GetByPhoneAsync(string normalizedPhone, CancellationToken ct = default) =>
        await Set.FirstOrDefaultAsync(c => c.Phone.Value == normalizedPhone, ct);

    public async Task<bool> PhoneExistsAsync(string normalizedPhone, CancellationToken ct = default) =>
        await Set.AnyAsync(c => c.Phone.Value == normalizedPhone, ct);

    public async Task<PagedResult<Customer>> SearchAsync(
        string? search, int page, int pageSize, CancellationToken ct = default)
    {
        var query = Set.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(c =>
                c.Name.Contains(term) ||
                c.Phone.Value.Contains(term));
        }

        var total = await query.CountAsync(ct);

        var items = await query
            .OrderBy(c => c.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return new PagedResult<Customer>
        {
            Items = items,
            TotalCount = total,
            Page = page,
            PageSize = pageSize
        };
    }
}
