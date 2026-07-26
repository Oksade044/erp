using ERP.Application.Common.Interfaces;
using ERP.Domain.Modules.Users;

namespace ERP.Application.Modules.Auth;

/// <summary>
/// Rol adına görə icazələri həll edir (#16). Əvvəlcə dinamik AppRole-dan; tapılmasa
/// daxili Permissions.ForRole enum xəritəsinə düşür (təhlükəsizlik ehtiyatı).
/// </summary>
public static class AuthPermissions
{
    public static async Task<IReadOnlyList<string>> ResolveAsync(
        IRoleRepository roles, string roleName, CancellationToken ct)
    {
        var role = await roles.GetByNameAsync(roleName, ct);
        if (role is not null)
            return role.Permissions.ToList();

        // Ehtiyat: AppRole hələ seed olunmayıbsa, enum əsaslı standart icazələr.
        return Enum.TryParse<Role>(roleName, ignoreCase: true, out var parsed)
            ? Permissions.ForRole(parsed).ToList()
            : [];
    }
}
