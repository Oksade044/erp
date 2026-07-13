using ERP.Domain.Modules.Users;

namespace ERP.Application.Common.Interfaces;

/// <summary>İstifadəçiyə xas repository (TDD §14).</summary>
public interface IUserRepository : IRepository<User>
{
    Task<User?> GetByUsernameAsync(string username, CancellationToken ct = default);
    Task<User?> GetByRefreshTokenAsync(string refreshToken, CancellationToken ct = default);
    Task<bool> AnyAsync(CancellationToken ct = default);
}
