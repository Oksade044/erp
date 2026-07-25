using ERP.Domain.Common;
using ERP.Domain.Exceptions;
using ERP.Domain.Modules.Users;

namespace ERP.Domain.Modules.Settings;

/// <summary>
/// Sahə-səviyyəli görünürlük qaydası (TDD §7) — bir həssas sahəni hansı rolların GÖRƏ bildiyi.
/// Admin/Menecer tənzimləyir; kod dəyişmədən icazələr idarə olunur. Rollar CSV kimi saxlanılır.
/// </summary>
public class FieldPermission : BaseEntity, IAggregateRoot
{
    public string FieldKey { get; private set; } = null!;

    /// <summary>Görə bilən rollar — vergüllə ayrılmış (məs. "Admin,Menecer"). EF bunu map edir.</summary>
    public string AllowedRolesCsv { get; private set; } = "";

    /// <summary>Görə bilən rollar (parse olunmuş). DB-yə map olunmur.</summary>
    public IReadOnlyCollection<Role> AllowedRoles => Parse(AllowedRolesCsv);

    // EF Core üçün.
    private FieldPermission() { }

    public static FieldPermission Create(string fieldKey, IEnumerable<Role> roles)
    {
        if (string.IsNullOrWhiteSpace(fieldKey))
            throw new DomainException("Sahə açarı tələb olunur.");

        return new FieldPermission { FieldKey = fieldKey.Trim() }.SetRoles(roles);
    }

    public FieldPermission SetRoles(IEnumerable<Role> roles)
    {
        AllowedRolesCsv = string.Join(",", roles.Distinct().Select(r => r.ToString()));
        return this;
    }

    /// <summary>Verilmiş rol bu sahəni görə bilirmi? Admin həmişə görür.</summary>
    public bool CanView(Role role) => role == Role.Admin || AllowedRoles.Contains(role);

    private static IReadOnlyCollection<Role> Parse(string csv)
    {
        if (string.IsNullOrWhiteSpace(csv)) return [];
        var result = new List<Role>();
        foreach (var part in csv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            if (Enum.TryParse<Role>(part, out var role))
                result.Add(role);
        return result;
    }
}
