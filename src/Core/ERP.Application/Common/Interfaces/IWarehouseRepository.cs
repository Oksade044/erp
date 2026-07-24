using ERP.Application.Common.Models;
using ERP.Domain.Modules.Warehouses;

namespace ERP.Application.Common.Interfaces;

/// <summary>Anbara xas repository (TDD §14).</summary>
public interface IWarehouseRepository : IRepository<Warehouse>
{
    Task<bool> CodeExistsAsync(string normalizedCode, CancellationToken ct = default);

    Task<PagedResult<Warehouse>> SearchAsync(
        string? search, int page, int pageSize, CancellationToken ct = default);
}
