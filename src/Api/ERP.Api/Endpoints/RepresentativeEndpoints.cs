using ERP.Application.Common.Messaging;
using ERP.Application.Modules.Representatives;
using ERP.Domain.Modules.Users;
using ERP.Shared.Contracts.Representatives;

namespace ERP.Api.Endpoints;

/// <summary>Təmsilçi-borc sistemi endpoint-ləri (#16-18). Admin borc təyin edir, balanslar izlənir.</summary>
public static class RepresentativeEndpoints
{
    public static IEndpointRouteBuilder MapRepresentativeEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/representatives").WithTags("Representatives");

        // Bütün təmsilçilərin cari balansı.
        group.MapGet("/", async (ISender sender) =>
            Results.Ok(await sender.Send(new GetRepresentativeBalancesQuery())))
            .RequireAuthorization(Permissions.HrView);

        // Bir təmsilçinin defteri (balans + qeydlər).
        group.MapGet("/{name}/ledger", async (string name, ISender sender) =>
        {
            var result = await sender.Send(new GetRepresentativeLedgerQuery(Uri.UnescapeDataString(name)));
            return result.IsSuccess ? Results.Ok(result.Value) : Results.NotFound(new { error = result.Error });
        }).RequireAuthorization(Permissions.HrView);

        // Admin təmsilçiyə borc təyin edir.
        group.MapPost("/debt", async (AssignDebtRequest request, ISender sender) =>
        {
            var result = await sender.Send(new AssignDebtCommand(request));
            return result.IsSuccess ? Results.Ok() : Results.BadRequest(new { error = result.Error });
        }).RequireAuthorization(Permissions.HrEdit);

        return app;
    }
}
