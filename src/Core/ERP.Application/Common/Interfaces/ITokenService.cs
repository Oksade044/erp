using ERP.Domain.Modules.Users;

namespace ERP.Application.Common.Interfaces;

/// <summary>JWT token generasiyası (TDD §6). Access qısa ömürlü, refresh uzun.</summary>
public interface ITokenService
{
    /// <summary>Access + refresh token. İcazələr rola görə çöldən verilir (#16 — dinamik).</summary>
    (string accessToken, string refreshToken, DateTimeOffset expiresAt) GenerateTokens(
        User user, IReadOnlyCollection<string> permissions);
}
