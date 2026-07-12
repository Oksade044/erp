using ERP.Application.Common.Models;
using ERP.Domain.Modules.Orders;

namespace ERP.Application.Common.Interfaces;

/// <summary>İcarə sifarişinə xas repository (TDD §14).</summary>
public interface IRentalOrderRepository : IRepository<RentalOrder>
{
    /// <summary>Sifarişi sətirləri ilə birlikdə gətirir.</summary>
    Task<RentalOrder?> GetByIdWithLinesAsync(Guid id, CancellationToken ct = default);

    Task<PagedResult<RentalOrder>> SearchAsync(
        string? search, int page, int pageSize, CancellationToken ct = default);

    /// <summary>
    /// Verilmiş məhsul üçün, verilmiş tarix aralığı ilə üst-üstə düşən və anbarı rezerv edən
    /// (təsdiqlənmiş/təhvil verilmiş) DİGƏR sifarişlərdə rezerv olunmuş ümumi say.
    /// İkiqat-bron yoxlaması üçün (TDD §38).
    /// </summary>
    Task<int> GetReservedQuantityAsync(
        Guid productId, DateOnly startDate, DateOnly endDate, Guid excludeOrderId,
        CancellationToken ct = default);
}
