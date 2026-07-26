using ERP.Domain.Common;
using ERP.Domain.Exceptions;

namespace ERP.Domain.Modules.Users;

/// <summary>
/// Dinamik rol (#16) — Admin yeni rol yarada və hər rol üçün icazələri təyin edə bilər.
/// Sistem rolları (Admin/Menecer/Anbardar/Kassir) silinə bilməz (IsSystem). İcazələr CSV kimi.
/// </summary>
public class AppRole : BaseEntity, IAggregateRoot
{
    public string Name { get; private set; } = null!;
    public string PermissionsCsv { get; private set; } = "";

    /// <summary>Daxili (built-in) rol — silinə bilməz, adı dəyişməz.</summary>
    public bool IsSystem { get; private set; }

    public IReadOnlyCollection<string> Permissions => PermissionsCsv
        .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    // EF Core üçün.
    private AppRole() { }

    public static AppRole Create(string name, IEnumerable<string> permissions, bool isSystem = false)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Rol adı tələb olunur.");

        return new AppRole { Name = name.Trim(), IsSystem = isSystem }.SetPermissions(permissions);
    }

    public AppRole SetPermissions(IEnumerable<string> permissions)
    {
        PermissionsCsv = string.Join(",", permissions.Distinct().Where(p => !string.IsNullOrWhiteSpace(p)));
        return this;
    }

    public bool Has(string permission) => Permissions.Contains(permission);
}
