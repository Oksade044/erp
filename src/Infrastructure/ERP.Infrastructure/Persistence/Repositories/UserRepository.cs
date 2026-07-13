using ERP.Application.Common.Interfaces;
using ERP.Domain.Modules.Users;
using Microsoft.EntityFrameworkCore;

namespace ERP.Infrastructure.Persistence.Repositories;

/// <summary>İstifadəçiyə xas repository implementasiyası (TDD §14).</summary>
public sealed class UserRepository(AppDbContext context)
    : Repository<User>(context), IUserRepository
{
    public async Task<User?> GetByUsernameAsync(string username, CancellationToken ct = default) =>
        await Set.FirstOrDefaultAsync(u => u.Username == username, ct);

    public async Task<User?> GetByRefreshTokenAsync(string refreshToken, CancellationToken ct = default) =>
        await Set.FirstOrDefaultAsync(u => u.RefreshToken == refreshToken, ct);

    public async Task<bool> AnyAsync(CancellationToken ct = default) =>
        await Set.AnyAsync(ct);
}
