using ERP.Shared.Contracts.Products;

namespace ERP.Application.Common.Interfaces;

/// <summary>Məhsulun istifadə tarixçəsi (#38).</summary>
public interface IProductHistoryReader
{
    Task<IReadOnlyList<ProductHistoryRowDto>> GetAsync(Guid productId, CancellationToken ct = default);
}
