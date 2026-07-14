using ERP.Domain.Exceptions;
using ERP.Domain.Modules.Users;
using ERP.Shared.Contracts.Users;

namespace ERP.Application.Modules.Users;

/// <summary>User entity → DTO çevirmələri (parol sahələri heç vaxt çıxmır). TDD §12.</summary>
public static class UserMapping
{
    public static UserDto ToDto(this User u) => new(
        Id: u.Id,
        Username: u.Username,
        FullName: u.FullName,
        Role: u.Role.ToString(),
        IsActive: u.IsActive,
        CreatedAt: u.CreatedAt);

    public static Role ParseRole(string? role)
    {
        if (Enum.TryParse<Role>(role, ignoreCase: true, out var parsed))
            return parsed;
        throw new DomainException($"Rol düzgün deyil: {role}. (Admin | Menecer | Anbardar | Kassir)");
    }
}
