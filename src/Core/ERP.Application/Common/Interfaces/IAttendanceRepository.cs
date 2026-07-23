using ERP.Application.Common.Models;
using ERP.Domain.Modules.Hr;

namespace ERP.Application.Common.Interfaces;

/// <summary>Davamiyyətə xas repository (TDD §14).</summary>
public interface IAttendanceRepository : IRepository<Attendance>
{
    /// <summary>Verilmiş işçi üçün verilmiş gündə qeyd varmı (unikal: bir gün bir qeyd).</summary>
    Task<bool> ExistsForEmployeeDateAsync(Guid employeeId, DateOnly date, CancellationToken ct = default);

    /// <summary>Axtarış (işçi adı) + işçi filtri + səhifələmə (TDD §11).</summary>
    Task<PagedResult<Attendance>> SearchAsync(
        string? search, Guid? employeeId, int page, int pageSize, CancellationToken ct = default);
}
