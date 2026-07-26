using ERP.Shared.Contracts.Products;

namespace ERP.Application.Common.Interfaces;

/// <summary>Məhsulun anbarlar üzrə mövcudluğu (#18/#19) — rezerv/kirayə/boş hesablanır.</summary>
public interface IAvailabilityReader
{
    Task<IReadOnlyList<ProductAvailabilityDto>> GetProductAvailabilityAsync(Guid productId, CancellationToken ct = default);

    /// <summary>Verilmiş məhsullar üçün yekun stok xülasəsi (#27 — siyahıda göstərmək üçün, bir sorğuda).</summary>
    Task<IReadOnlyDictionary<Guid, StockSummaryDto>> GetSummariesAsync(
        IReadOnlyCollection<Guid> productIds, CancellationToken ct = default);
}
