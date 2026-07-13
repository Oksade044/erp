namespace ERP.Shared.Contracts.Auth;

/// <summary>Giriş sorğusu.</summary>
public sealed record LoginRequest(string Username, string Password);

/// <summary>Token yeniləmə sorğusu.</summary>
public sealed record RefreshRequest(string RefreshToken);

/// <summary>Uğurlu autentifikasiya cavabı — tokenlər + istifadəçi məlumatı.</summary>
public sealed record AuthResponse(
    string AccessToken,
    string RefreshToken,
    DateTimeOffset ExpiresAt,
    string Username,
    string FullName,
    string Role,
    IReadOnlyList<string> Permissions);
