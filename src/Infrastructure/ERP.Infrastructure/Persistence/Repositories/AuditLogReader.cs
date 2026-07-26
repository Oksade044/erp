using ERP.Application.Common.Interfaces;
using ERP.Application.Common.Models;
using ERP.Shared.Contracts.Audit;
using Microsoft.EntityFrameworkCore;

namespace ERP.Infrastructure.Persistence.Repositories;

/// <summary>Audit jurnalı oxuyucusu (#26) — birbaşa AppDbContext-dən.</summary>
public sealed class AuditLogReader(AppDbContext context) : IAuditLogReader
{
    public async Task<PagedResult<AuditLogDto>> SearchAsync(
        string? search, int page, int pageSize, CancellationToken ct = default)
    {
        var query = context.AuditLogs.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(a =>
                a.UserName.Contains(term) ||
                a.EntityType.Contains(term) ||
                a.Action.Contains(term));
        }

        var total = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(a => a.TimestampTicks)  // provider-safe (long)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(a => new AuditLogDto(
                a.Id, a.Timestamp, a.UserName, a.Action, a.EntityType, a.EntityId, a.Summary))
            .ToListAsync(ct);

        return new PagedResult<AuditLogDto>
        {
            Items = items, TotalCount = total, Page = page, PageSize = pageSize
        };
    }
}
