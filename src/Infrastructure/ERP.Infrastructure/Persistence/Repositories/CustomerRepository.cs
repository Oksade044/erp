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
        var all = await Set.AsNoTracking().ToListAsync(ct);
        return RankedSearch.Page(all, search, page, pageSize,
            primary: c => c.Name,
            secondary: c => [c.Phone.Value, c.Address?.City ?? "", c.RepresentativeName ?? ""]);
    }

    public async Task<IReadOnlyList<Customer>> GetDebtorsAsync(CancellationToken ct = default) =>
        // Debt owned VO borc 0 olanda NULL saxlanır → null-check provider-safe (SQLite decimal=TEXT
        // müqayisə problemini yan keçir) və miqyaslanandır (yalnız borclular DB-dən gəlir).
        await Set.AsNoTracking().Where(c => c.Debt != null).ToListAsync(ct);
}
