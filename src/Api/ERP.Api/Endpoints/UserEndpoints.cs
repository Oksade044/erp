using ERP.Application.Common.Messaging;
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

        return app;
    }
}
