using ERP.Application.Common.Models;
using ERP.Domain.Modules.Warehouses;

namespace ERP.Application.Common.Interfaces;

/// <summary>Stok səviyyəsinə xas repository (TDD §14).</summary>
public interface IStockLevelRepository : IRepository<StockLevel>
{
    /// <summary>Verilmiş məhsul + anbar cütü üçün səviyyə (yoxdursa null).</summary>
    Task<StockLevel?> GetAsync(Guid productId, Guid warehouseId, CancellationToken ct = default);

    /// <summary>Verilmiş məhsullar üçün bütün anbar səviyyələri (siyahıda anbar-stok göstərmək üçün).</summary>
    Task<IReadOnlyList<StockLevel>> ListByProductsAsync(IReadOnlyCollection<Guid> productIds, CancellationToken ct = default);

    /// <summary>Səviyyələr — anbar filtri + yalnız-aşağı filtri + səhifələmə (TDD §11).</summary>
    Task<PagedResult<StockLevel>> SearchAsync(
        string? search, Guid? warehouseId, bool lowOnly, int page, int pageSize, CancellationToken ct = default);
}
