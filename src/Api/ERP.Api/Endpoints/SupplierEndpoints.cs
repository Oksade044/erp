using ERP.Application.Common.Messaging;
using ERP.Application.Modules.Suppliers.Commands;
using ERP.Application.Modules.Suppliers.Queries;
using ERP.Domain.Modules.Users;
using ERP.Shared.Contracts.Suppliers;

namespace ERP.Api.Endpoints;

/// <summary>
/// Təchizatçı API endpoint-ləri — versiyalı, RESTful (TDD §11). Nazik controller (TDD §16).
/// Oxu → suppliers.view, dəyişiklik → suppliers.edit (permission-based RBAC, TDD §7).
/// </summary>
public static class SupplierEndpoints
{
    public static IEndpointRouteBuilder MapSupplierEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/suppliers").WithTags("Suppliers");

        group.MapGet("/", async (string? search, ISender sender, int page = 1, int pageSize = 20) =>
        {
            var result = await sender.Send(new GetSuppliersQuery(search, page, pageSize));
            return Results.Ok(result);
        }).RequireAuthorization(Permissions.SuppliersView);

        group.MapGet("/{id:guid}", async (Guid id, ISender sender) =>
        {
            var result = await sender.Send(new GetSupplierByIdQuery(id));
            return result.IsSuccess ? Results.Ok(result.Value) : Results.NotFound(new { error = result.Error });
        }).RequireAuthorization(Permissions.SuppliersView);

        group.MapPost("/", async (CreateSupplierRequest request, ISender sender) =>
        {
            var result = await sender.Send(new CreateSupplierCommand(request));
            return result.IsSuccess
                ? Results.Created($"/api/v1/suppliers/{result.Value}", new { id = result.Value })
                : Results.BadRequest(new { error = result.Error });
        }).RequireAuthorization(Permissions.SuppliersEdit);

        group.MapPut("/{id:guid}", async (Guid id, UpdateSupplierRequest request, ISender sender) =>
        {
            var result = await sender.Send(new UpdateSupplierCommand(id, request));
            return result.IsSuccess ? Results.NoContent() : Results.BadRequest(new { error = result.Error });
        }).RequireAuthorization(Permissions.SuppliersEdit);

        return app;
    }
}
