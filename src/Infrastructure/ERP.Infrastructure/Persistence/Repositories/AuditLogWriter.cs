using ERP.Application.Common.Interfaces;
using ERP.Domain.Modules.Audit;

namespace ERP.Infrastructure.Persistence.Repositories;

/// <summary>Audit jurnalına açıq qeyd yazır (#32) — cari SaveChanges ilə saxlanılır.</summary>
public sealed class AuditLogWriter(AppDbContext context) : IAuditLogWriter
{
    public void Add(string userName, string action, string entityType, string entityId, string? summary) =>
        context.Set<AuditLog>().Add(AuditLog.Create(
            DateTimeOffset.UtcNow, userName, action, entityType, entityId, summary));
}
