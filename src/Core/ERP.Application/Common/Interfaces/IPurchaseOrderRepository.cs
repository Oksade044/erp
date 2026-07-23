using ERP.Application.Common.Models;
using ERP.Domain.Modules.Purchases;

namespace ERP.Application.Common.Interfaces;

/// <summary>Alış sifarişinə xas repository (TDD §14).</summary>
public interface IPurchaseOrderRepository : IRepository<PurchaseOrder>
{
    /// <summary>Alışı sətirləri ilə birlikdə gətirir.</summary>
    Task<PurchaseOrder?> GetByIdWithLinesAsync(Guid id, CancellationToken ct = default);

    Task<PagedResult<PurchaseOrder>> SearchAsync(
        string? search, int page, int pageSize, CancellationToken ct = default);
}
