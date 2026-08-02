using ERP.Application.Common.Messaging;
using ERP.Application.Modules.Warehouses.Commands;
using ERP.Application.Modules.Warehouses.Queries;
using ERP.Domain.Modules.Users;
using ERP.Shared.Contracts.Warehouses;

namespace ERP.Api.Endpoints;

/// <summary>
/// Anbar (Warehouse) API endpoint-ləri — versiyalı, RESTful (TDD §11). Nazik controller.
/// Oxu → warehouses.view, dəyişiklik → warehouses.edit (RBAC, TDD §7).
/// </summary>
public static class WarehouseEndpoints
{
    public static IEndpointRouteBuilder MapWarehouseEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/warehouses").WithTags("Warehouses");

        group.MapGet("/", async (string? search, ISender sender, int page = 1, int pageSize = 20) =>
        {
            var result = await sender.Send(new GetWarehousesQuery(search, page, pageSize));
            return Results.Ok(result);
        }).RequireAuthorization(Permissions.WarehousesView);

        group.MapGet("/{id:guid}", async (Guid id, ISender sender) =>
        {
            var result = await sender.Send(new GetWarehouseByIdQuery(id));
            return result.IsSuccess ? Results.Ok(result.Value) : Results.NotFound(new { error = result.Error });
        }).RequireAuthorization(Permissions.WarehousesView);

        group.MapPost("/", async (CreateWarehouseRequest request, ISender sender) =>
        {
            var result = await sender.Send(new CreateWarehouseCommand(request));
            return result.IsSuccess
                ? Results.Created($"/api/v1/warehouses/{result.Value}", new { id = result.Value })
                : Results.BadRequest(new { error = result.Error });
        }).RequireAuthorization(Permissions.WarehousesEdit);

        group.MapPut("/{id:guid}", async (Guid id, UpdateWarehouseRequest request, ISender sender) =>
        {
            var result = await sender.Send(new UpdateWarehouseCommand(id, request));
            return result.IsSuccess ? Results.NoContent() : Results.BadRequest(new { error = result.Error });
        }).RequireAuthorization(Permissions.WarehousesEdit);

        group.MapDelete("/{id:guid}", async (Guid id, ISender sender) =>
        {
            var result = await sender.Send(new ERP.Application.Modules.Warehouses.Commands.DeleteWarehouseCommand(id));
            return result.IsSuccess ? Results.NoContent() : Results.BadRequest(new { error = result.Error });
        }).RequireAuthorization(Permissions.WarehousesEdit);

        return app;
    }
}
