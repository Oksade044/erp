using ERP.Domain.Modules.Users;

namespace ERP.Application.Common.Interfaces;

/// <summary>JWT token generasiyası (TDD §6). Access qısa ömürlü, refresh uzun.</summary>
public interface ITokenService
{
    (string accessToken, string refreshToken, DateTimeOffset expiresAt) GenerateTokens(User user);
}
