using ERP.Domain.Common;
using ERP.Domain.Exceptions;

namespace ERP.Domain.Modules.Users;

/// <summary>
/// İstifadəçi — aggregate root (TDD §6, §7). Parol yalnız hash+salt kimi saxlanılır (heç vaxt açıq).
/// Rol atomik icazələri müəyyən edir. Refresh token burada saxlanılır (stateless access + davamlı refresh).
/// </summary>
public class User : BaseEntity, IAggregateRoot
{
    public string Username { get; private set; } = null!;
    public string PasswordHash { get; private set; } = null!;
    public string PasswordSalt { get; private set; } = null!;
    public string FullName { get; private set; } = null!;
    public Role Role { get; private set; }
    public bool IsActive { get; private set; } = true;

    public string? RefreshToken { get; private set; }
    public DateTimeOffset? RefreshTokenExpiresAt { get; private set; }

    // EF Core üçün.
    private User() { }

    private User(string username, string hash, string salt, string fullName, Role role)
    {
        Username = username;
        PasswordHash = hash;
        PasswordSalt = salt;
        FullName = fullName;
        Role = role;
    }

    public static User Create(string username, string passwordHash, string passwordSalt, string fullName, Role role)
    {
        if (string.IsNullOrWhiteSpace(username))
            throw new DomainException("İstifadəçi adı tələb olunur.");
        if (string.IsNullOrWhiteSpace(passwordHash) || string.IsNullOrWhiteSpace(passwordSalt))
            throw new DomainException("Parol hash-i tələb olunur.");
        if (string.IsNullOrWhiteSpace(fullName))
            throw new DomainException("Ad tələb olunur.");

        return new User(username.Trim().ToLowerInvariant(), passwordHash, passwordSalt, fullName.Trim(), role);
    }

    public void SetRefreshToken(string token, DateTimeOffset expiresAt)
    {
        RefreshToken = token;
        RefreshTokenExpiresAt = expiresAt;
    }

    public void ClearRefreshToken()
    {
        RefreshToken = null;
        RefreshTokenExpiresAt = null;
    }

    public bool IsRefreshTokenValid(string token) =>
        RefreshToken is not null
        && RefreshToken == token
        && RefreshTokenExpiresAt is not null
        && RefreshTokenExpiresAt > DateTimeOffset.UtcNow;

    public void Deactivate() => IsActive = false;
    public void Activate() => IsActive = true;

    public IReadOnlyCollection<string> GetPermissions() => Permissions.ForRole(Role);
}
