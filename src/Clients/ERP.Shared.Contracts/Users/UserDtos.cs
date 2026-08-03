namespace ERP.Shared.Contracts.Users;

/// <summary>İstifadəçi cavab DTO-su. Parol (hash/salt/açıq) heç vaxt şəbəkəyə çıxmır. TDD §12.</summary>
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
