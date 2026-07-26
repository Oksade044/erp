using ERP.Application.Common.Messaging;
using ERP.Application.Modules.Audit.Queries;
using ERP.Domain.Modules.Users;

namespace ERP.Api.Endpoints;

/// <summary>Audit jurnalı (#26) — yalnız səlahiyyətli istifadəçilər (audit.view, adətən Admin).</summary>
public static class AuditEndpoints
{
    public static IEndpointRouteBuilder MapAuditEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/audit").WithTags("Audit")
            .RequireAuthorization(Permissions.AuditView);

        group.MapGet("/", async (string? search, ISender sender, int page = 1, int pageSize = 50) =>
            Results.Ok(await sender.Send(new GetAuditLogsQuery(search, page, pageSize))));

        return app;
    }
}
