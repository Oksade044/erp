using ERP.Application.Common.Messaging;
using ERP.Application.Modules.Purchases.Commands;
using ERP.Application.Modules.Purchases.Queries;
using ERP.Domain.Modules.Users;
using ERP.Shared.Contracts.Purchases;

namespace ERP.Api.Endpoints;

/// <summary>
/// Alış (Purchase) API endpoint-ləri — versiyalı, RESTful (TDD §11). Nazik controller (TDD §16).
/// Oxu → purchases.view, dəyişiklik/status → purchases.edit (RBAC, TDD §7).
/// Qəbul (receive) məhsul stokunu artıran addımdır.
/// </summary>
public static class PurchaseEndpoints
{
    public static IEndpointRouteBuilder MapPurchaseEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/purchases").WithTags("Purchases");

        group.MapGet("/", async (string? search, ISender sender, int page = 1, int pageSize = 20) =>
        {
            var result = await sender.Send(new GetPurchasesQuery(search, page, pageSize));
            return Results.Ok(result);
        }).RequireAuthorization(Permissions.PurchasesView);

        group.MapGet("/{id:guid}", async (Guid id, ISender sender) =>
        {
            var result = await sender.Send(new GetPurchaseByIdQuery(id));
            return result.IsSuccess ? Results.Ok(result.Value) : Results.NotFound(new { error = result.Error });
        }).RequireAuthorization(Permissions.PurchasesView);

        group.MapPost("/", async (CreatePurchaseRequest request, ISender sender) =>
        {
            var result = await sender.Send(new CreatePurchaseCommand(request));
            return result.IsSuccess
                ? Results.Created($"/api/v1/purchases/{result.Value}", new { id = result.Value })
                : Results.BadRequest(new { error = result.Error });
        }).RequireAuthorization(Permissions.PurchasesEdit);

        group.MapPost("/{id:guid}/confirm", async (Guid id, ISender sender) =>
        {
            var result = await sender.Send(new ConfirmPurchaseCommand(id));
            return result.IsSuccess ? Results.NoContent() : Results.BadRequest(new { error = result.Error });
        }).RequireAuthorization(Permissions.PurchasesEdit);

        group.MapPost("/{id:guid}/receive", async (Guid id, ISender sender) =>
        {
            var result = await sender.Send(new ReceivePurchaseCommand(id));
            return result.IsSuccess ? Results.NoContent() : Results.BadRequest(new { error = result.Error });
        }).RequireAuthorization(Permissions.PurchasesEdit);

        group.MapPost("/{id:guid}/cancel", async (Guid id, ISender sender) =>
        {
            var result = await sender.Send(new CancelPurchaseCommand(id));
            return result.IsSuccess ? Results.NoContent() : Results.BadRequest(new { error = result.Error });
        }).RequireAuthorization(Permissions.PurchasesEdit);

        return app;
    }
}
