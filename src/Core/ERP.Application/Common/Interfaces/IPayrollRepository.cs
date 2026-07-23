using ERP.Application.Common.Models;
using ERP.Domain.Modules.Hr;

namespace ERP.Application.Common.Interfaces;

/// <summary>Əməkhaqqıya xas repository (TDD §14).</summary>
public interface IPayrollRepository : IRepository<Payroll>
{
    /// <summary>Verilmiş işçi üçün verilmiş dövrdə (il+ay) hesablama varmı (unikal).</summary>
    Task<bool> ExistsForPeriodAsync(Guid employeeId, int year, int month, CancellationToken ct = default);

    Task<PagedResult<Payroll>> SearchAsync(
        string? search, Guid? employeeId, int page, int pageSize, CancellationToken ct = default);
}
