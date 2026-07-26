namespace ERP.Domain.Modules.Audit;

/// <summary>
/// Audit jurnalı qeydi (#26 / TDD §20) — sistemdə edilən hər əməliyyat: kim, nə vaxt,
/// hansı obyekt, hansı əməliyyat, hansı dəyişikliklər. BaseEntity DEYİL (öz-özünü audit etməsin).
/// </summary>
public class AuditLog
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public DateTimeOffset Timestamp { get; private set; }

    /// <summary>Sıralama üçün (SQLite DateTimeOffset-i ORDER BY-da dəstəkləmir — long istifadə olunur).</summary>
    public long TimestampTicks { get; private set; }

    /// <summary>Əməliyyatı edən istifadəçi (ad).</summary>
    public string UserName { get; private set; } = null!;

    /// <summary>Yaradıldı / Dəyişdirildi / Silindi.</summary>
    public string Action { get; private set; } = null!;

    /// <summary>Obyektin növü (məs. Product, RentalOrder, Invoice).</summary>
    public string EntityType { get; private set; } = null!;

    /// <summary>Obyektin identifikatoru.</summary>
    public string EntityId { get; private set; } = null!;

    /// <summary>Dəyişən sahələrin xülasəsi (Modified üçün).</summary>
    public string? Summary { get; private set; }

    // EF Core üçün.
    private AuditLog() { }

    public static AuditLog Create(
        DateTimeOffset timestamp, string userName, string action,
        string entityType, string entityId, string? summary) => new()
    {
        Timestamp = timestamp,
        TimestampTicks = timestamp.UtcTicks,
        UserName = userName,
        Action = action,
        EntityType = entityType,
        EntityId = entityId,
        Summary = summary
    };
}
