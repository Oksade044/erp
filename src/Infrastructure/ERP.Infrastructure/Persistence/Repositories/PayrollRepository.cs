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

    /// <summary>Ödənişlərlə birlikdə izlənən şəkildə yükləyir (installment status/qalıq üçün).</summary>
    public async Task<Payroll?> GetByIdWithPaymentsAsync(Guid id, CancellationToken ct = default) =>
        await Set.Include(p => p.Payments).FirstOrDefaultAsync(p => p.Id == id, ct);

    /// <summary>Ödəniş qeydini ayrıca DbSet-ə əlavə edir — həmişə "Added" (INSERT).</summary>
    public void AddPaymentRecord(PayrollPayment payment) =>
        Context.Set<PayrollPayment>().Add(payment);

    public async Task<PagedResult<Payroll>> SearchAsync(
        string? search, Guid? employeeId, int page, int pageSize, CancellationToken ct = default)
    {
        var query = Set.AsNoTracking().Include(p => p.Payments).AsQueryable();

        if (employeeId is { } id)
            query = query.Where(p => p.EmployeeId == id);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var all = await query.ToListAsync(ct);
            return RankedSearch.Page(all, search, page, pageSize,
                primary: p => p.EmployeeName,
                secondary: p => [p.PayrollNumber]);
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
