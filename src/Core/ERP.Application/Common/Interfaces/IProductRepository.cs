using ERP.Application.Common.Models;
using ERP.Domain.Modules.Products;

namespace ERP.Application.Common.Interfaces;

/// <summary>Məhsula xas repository (TDD §14).</summary>
public interface IProductRepository : IRepository<Product>
{
    Task<bool> SkuExistsAsync(string normalizedSku, CancellationToken ct = default);

    Task<PagedResult<Product>> SearchAsync(
        string? search, int page, int pageSize, CancellationToken ct = default);
}
