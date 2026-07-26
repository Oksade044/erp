using ERP.Application.Common.Interfaces;
using ERP.Application.Common.Messaging;
using ERP.Application.Common.Models;
using ERP.Shared.Contracts.Audit;

namespace ERP.Application.Modules.Audit.Queries;

/// <summary>Audit jurnalı — axtarış + səhifələmə (#26).</summary>
public sealed record GetAuditLogsQuery(string? Search, int Page = 1, int PageSize = 50)
    : IRequest<PagedResult<AuditLogDto>>;

public sealed class GetAuditLogsHandler(IAuditLogReader reader)
    : IRequestHandler<GetAuditLogsQuery, PagedResult<AuditLogDto>>
{
    public Task<PagedResult<AuditLogDto>> Handle(GetAuditLogsQuery request, CancellationToken ct)
    {
        var page = request.Page < 1 ? 1 : request.Page;
        var size = request.PageSize is < 1 or > 200 ? 50 : request.PageSize;
        return reader.SearchAsync(request.Search, page, size, ct);
    }
}
