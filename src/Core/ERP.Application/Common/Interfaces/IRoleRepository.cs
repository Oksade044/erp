using ERP.Domain.Modules.Users;

namespace ERP.Application.Common.Interfaces;

/// <summary>Dinamik rollara xas repository (#16).</summary>
public interface IRoleRepository : IRepository<AppRole>
{
    Task<AppRole?> GetByNameAsync(string name, CancellationToken ct = default);
    Task<IReadOnlyList<AppRole>> ListOrderedAsync(CancellationToken ct = default);
    Task<bool> AnyAsync(CancellationToken ct = default);
}
