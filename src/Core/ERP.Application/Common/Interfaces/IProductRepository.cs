using ERP.Application.Common.Models;
using ERP.Domain.Modules.Products;

namespace ERP.Application.Common.Interfaces;

/// <summary>Məhsula xas repository (TDD §14).</summary>
public interface IProductRepository : IRepository<Product>
{
    Task<bool> SkuExistsAsync(string normalizedSku, CancellationToken ct = default);

    /// <summary>Növbəti avtomatik SKU-nu qaytarır: PRD-000001, PRD-000002, ... (unikal).</summary>
    Task<string> GenerateNextSkuAsync(CancellationToken ct = default);

    Task<PagedResult<Product>> SearchAsync(
        string? search, int page, int pageSize, CancellationToken ct = default);
}
