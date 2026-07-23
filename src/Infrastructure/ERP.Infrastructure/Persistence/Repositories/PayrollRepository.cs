using ERP.Application.Common.Interfaces;
using ERP.Application.Common.Models;
using ERP.Domain.Modules.Hr;
using Microsoft.EntityFrameworkCore;

namespace ERP.Infrastructure.Persistence.Repositories;

/// <summary>Əməkhaqqıya xas repository implementasiyası (TDD §14).</summary>
public sealed class PayrollRepository(AppDbContext context)
    : Repository<Payroll>(context), IPayrollRepository
{
    public async Task<bool> ExistsForPeriodAsync(Guid employeeId, int year, int month, CancellationToken ct = default) =>
        await Set.AnyAsync(p => p.EmployeeId == employeeId && p.Year == year && p.Month == month, ct);

    public async Task<PagedResult<Payroll>> SearchAsync(
        string? search, Guid? employeeId, int page, int pageSize, CancellationToken ct = default)
    {
        var query = Set.AsNoTracking().AsQueryable();

        if (employeeId is { } id)
            query = query.Where(p => p.EmployeeId == id);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(p => p.EmployeeName.Contains(term) || p.PayrollNumber.Contains(term));
        }

        var total = await query.CountAsync(ct);

        // Ən yeni dövr öndə (il, sonra ay). Hər iki provider-də təhlükəsiz int sıralama.
        var items = await query
            .OrderByDescending(p => p.Year)
            .ThenByDescending(p => p.Month)
            .ThenBy(p => p.EmployeeName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return new PagedResult<Payroll>
        {
            Items = items,
            TotalCount = total,
            Page = page,
            PageSize = pageSize
        };
    }
}
