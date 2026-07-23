using ERP.Application.Common.Models;
using ERP.Domain.Modules.Hr;

namespace ERP.Application.Common.Interfaces;

/// <summary>İşçiyə xas repository (TDD §14).</summary>
public interface IEmployeeRepository : IRepository<Employee>
{
    Task<bool> PhoneExistsAsync(string normalizedPhone, CancellationToken ct = default);

    Task<PagedResult<Employee>> SearchAsync(
        string? search, int page, int pageSize, CancellationToken ct = default);
}
