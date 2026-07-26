using ERP.Shared.Contracts.Products;

namespace ERP.Application.Common.Interfaces;

/// <summary>Məhsulun anbarlar üzrə mövcudluğu (#18/#19) — rezerv/kirayə/boş hesablanır.</summary>
public interface IAvailabilityReader
{
    Task<IReadOnlyList<ProductAvailabilityDto>> GetProductAvailabilityAsync(Guid productId, CancellationToken ct = default);
}
