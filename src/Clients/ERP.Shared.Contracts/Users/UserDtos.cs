namespace ERP.Shared.Contracts.Users;

/// <summary>İstifadəçi cavab DTO-su (parol heç vaxt qaytarılmır). TDD §12.</summary>
public sealed record UserDto(
    Guid Id,
    string Username,
    string FullName,
    string Role,
    bool IsActive,
    DateTimeOffset CreatedAt);

/// <summary>Yeni istifadəçi yaratmaq üçün request. Role: "Admin" | "Menecer" | "Anbardar" | "Kassir".</summary>
public sealed record CreateUserRequest(
    string Username,
    string Password,
    string FullName,
    string Role);

/// <summary>İstifadəçini yeniləmək üçün request (rol/aktivlik; parol opsional).</summary>
public sealed record UpdateUserRequest(
    string FullName,
    string Role,
    bool IsActive,
    string? NewPassword = null);
