using ERP.Application.Common.Interfaces;
using ERP.Application.Common.Models;
using ERP.Domain.Modules.Hr;
using Microsoft.EntityFrameworkCore;

namespace ERP.Infrastructure.Persistence.Repositories;

/// <summary>Davamiyyətə xas repository implementasiyası (TDD §14).</summary>
public sealed class AttendanceRepository(AppDbContext context)
    : Repository<Attendance>(context), IAttendanceRepository
{
    public async Task<bool> ExistsForEmployeeDateAsync(Guid employeeId, DateOnly date, CancellationToken ct = default) =>
        await Set.AnyAsync(a => a.EmployeeId == employeeId && a.Date == date, ct);

    public async Task<PagedResult<Attendance>> SearchAsync(
        string? search, Guid? employeeId, int page, int pageSize, CancellationToken ct = default)
    {
        var query = Set.AsNoTracking().AsQueryable();

        if (employeeId is { } id)
            query = query.Where(a => a.EmployeeId == id);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(a => a.EmployeeName.Contains(term));
        }

        var total = await query.CountAsync(ct);

        // Ən yeni tarix öndə. DateOnly hər iki provider-də ORDER BY-da təhlükəsizdir.
        var items = await query
            .OrderByDescending(a => a.Date)
            .ThenBy(a => a.EmployeeName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return new PagedResult<Attendance>
        {
            Items = items,
            TotalCount = total,
            Page = page,
            PageSize = pageSize
        };
    }
}
