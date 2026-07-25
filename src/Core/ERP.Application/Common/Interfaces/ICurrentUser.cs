namespace ERP.Application.Common.Interfaces;

/// <summary>
/// Cari istifadəçi konteksti — audit (TDD §20) və avtorizasiya (TDD §7) üçün.
/// API-də HTTP kontekstindən, digər hostlarda müvafiq mənbədən doldurulur.
/// </summary>
public interface ICurrentUser
{
    string? UserId { get; }
    string? UserName { get; }

    /// <summary>İstifadəçinin tam adı (JWT "fullName" iddiası) — "kim yaradıb" üçün.</summary>
    string? FullName { get; }

    /// <summary>İstifadəçinin rolu (Admin/Menecer/Anbardar/Kassir).</summary>
    string? Role { get; }

    bool IsAuthenticated { get; }
    bool HasPermission(string permission);
}
