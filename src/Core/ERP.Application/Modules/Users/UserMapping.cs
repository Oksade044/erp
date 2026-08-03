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
        Role: u.RoleName,
        IsActive: u.IsActive,
        CreatedAt: u.CreatedAt);
}
