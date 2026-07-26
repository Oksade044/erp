using ERP.Application.Common.Messaging;
using ERP.Application.Modules.Users;
using ERP.Application.Modules.Users.Commands;
using ERP.Application.Modules.Users.Queries;
using ERP.Domain.Modules.Users;
using ERP.Shared.Contracts.Users;

namespace ERP.Api.Endpoints;

/// <summary>İstifadəçi idarəetməsi — yalnız users.manage icazəsi (adətən Admin). TDD §7.</summary>
public static class UserEndpoints
{
    public static IEndpointRouteBuilder MapUserEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/users").WithTags("Users")
            .RequireAuthorization(Permissions.UsersManage);

        group.MapGet("/", async (ISender sender) =>
            Results.Ok(await sender.Send(new GetUsersQuery())));

        group.MapPost("/", async (CreateUserRequest request, ISender sender) =>
        {
            var result = await sender.Send(new CreateUserCommand(request));
            return result.IsSuccess
                ? Results.Created($"/api/v1/users/{result.Value}", new { id = result.Value })
                : Results.BadRequest(new { error = result.Error });
        });

        // --- Dinamik rollar (#16) ---
        var roles = app.MapGroup("/api/v1/roles").WithTags("Roles")
            .RequireAuthorization(Permissions.UsersManage);

        roles.MapGet("/", async (ISender sender) =>
            Results.Ok(await sender.Send(new GetRolesQuery())));

        roles.MapGet("/permissions", async (ISender sender) =>
            Results.Ok(await sender.Send(new GetPermissionCatalogQuery())));

        roles.MapPost("/", async (CreateRoleRequest request, ISender sender) =>
        {
            var result = await sender.Send(new CreateRoleCommand(request));
            return result.IsSuccess
                ? Results.Created($"/api/v1/roles/{result.Value}", new { id = result.Value })
                : Results.BadRequest(new { error = result.Error });
        });

        roles.MapPut("/{id:guid}/permissions", async (Guid id, UpdateRolePermissionsRequest request, ISender sender) =>
        {
            var result = await sender.Send(new UpdateRolePermissionsCommand(id, request));
            return result.IsSuccess ? Results.NoContent() : Results.BadRequest(new { error = result.Error });
        });

        roles.MapDelete("/{id:guid}", async (Guid id, ISender sender) =>
        {
            var result = await sender.Send(new DeleteRoleCommand(id));
            return result.IsSuccess ? Results.NoContent() : Results.BadRequest(new { error = result.Error });
        });

        return app;
    }
}
