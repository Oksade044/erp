using ERP.Domain.Modules.Products;

namespace ERP.Application.Common.Interfaces;

/// <summary>Məhsul kateqoriyalarına xas repository (TDD §14).</summary>
public interface ICategoryRepository : IRepository<Category>
{
    /// <summary>Ada görə kateqoriya (böyük/kiçik hərfə həssas deyil; yoxdursa null).</summary>
    Task<Category?> GetByNameAsync(string name, CancellationToken ct = default);

    /// <summary>Bütün kateqoriyalar — əlifba sırası ilə.</summary>
    Task<IReadOnlyList<Category>> ListOrderedAsync(CancellationToken ct = default);
}
