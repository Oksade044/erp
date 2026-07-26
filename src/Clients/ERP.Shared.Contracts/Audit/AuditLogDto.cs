namespace ERP.Shared.Contracts.Audit;

/// <summary>Audit jurnalı qeydi (#26).</summary>
public sealed record AuditLogDto(
    Guid Id,
    DateTimeOffset Timestamp,
    string UserName,
    string Action,
    string EntityType,
    string EntityId,
    string? Summary);
