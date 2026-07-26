namespace ERP.Shared.Contracts.Users;

/// <summary>Rol cavab DTO-su (#16).</summary>
public sealed record RoleDto(
    Guid Id,
    string Name,
    bool IsSystem,
    IReadOnlyList<string> Permissions);

/// <summary>İcazə kataloqu elementi — açar + aydın ad (matris UI üçün).</summary>
public sealed record PermissionInfoDto(string Key, string Label);

/// <summary>Yeni rol yaratmaq üçün request.</summary>
public sealed record CreateRoleRequest(string Name, IReadOnlyList<string> Permissions);

/// <summary>Rolun icazələrini yeniləmək üçün request.</summary>
public sealed record UpdateRolePermissionsRequest(IReadOnlyList<string> Permissions);
