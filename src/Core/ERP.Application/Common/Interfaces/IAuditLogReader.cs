using ERP.Application.Common.Models;
using ERP.Shared.Contracts.Audit;

namespace ERP.Application.Common.Interfaces;

/// <summary>Audit jurnalını oxumaq (#26). Yazma AuditInterceptor-da avtomatikdir.</summary>
public interface IAuditLogReader
{
    /// <summary>Jurnal qeydləri — ən yeni öndə, axtarış (istifadəçi/növ/əməliyyat) + səhifələmə.</summary>
    Task<PagedResult<AuditLogDto>> SearchAsync(string? search, string? entityId, int page, int pageSize, CancellationToken ct = default);
}
