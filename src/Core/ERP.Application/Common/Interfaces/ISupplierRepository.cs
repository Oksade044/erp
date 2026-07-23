using ERP.Application.Common.Models;
using ERP.Domain.Modules.Suppliers;

namespace ERP.Application.Common.Interfaces;

/// <summary>Təchizatçıya xas repository (TDD §14).</summary>
public interface ISupplierRepository : IRepository<Supplier>
{
    Task<bool> PhoneExistsAsync(string normalizedPhone, CancellationToken ct = default);

    /// <summary>Server-side axtarış + səhifələmə (TDD §11, §33).</summary>
    Task<PagedResult<Supplier>> SearchAsync(
        string? search, int page, int pageSize, CancellationToken ct = default);
}
