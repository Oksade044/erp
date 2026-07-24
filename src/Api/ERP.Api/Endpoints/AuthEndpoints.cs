using System.Security.Claims;
using ERP.Application.Common.Messaging;
using ERP.Application.Modules.Auth.Commands;
using ERP.Shared.Contracts.Auth;

namespace ERP.Api.Endpoints;

/// <summary>Autentifikasiya endpoint-ləri (TDD §6). login/refresh anonimdir, me qorunur.</summary>
public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/auth").WithTags("Auth");

        group.MapPost("/login", async (LoginRequest request, ISender sender) =>
        {
            var result = await sender.Send(new LoginCommand(request));
            return result.IsSuccess ? Results.Ok(result.Value) : Results.Unauthorized();
        }).AllowAnonymous().RequireRateLimiting("auth");

        group.MapPost("/refresh", async (RefreshRequest request, ISender sender) =>
        {
            var result = await sender.Send(new RefreshCommand(request));
            return result.IsSuccess ? Results.Ok(result.Value) : Results.Unauthorized();
        }).AllowAnonymous().RequireRateLimiting("auth");

        group.MapGet("/me", (ClaimsPrincipal user) => Results.Ok(new
        {
            username = user.Identity?.Name,
            fullName = user.FindFirstValue("fullName"),
            role = user.FindFirstValue(ClaimTypes.Role),
            permissions = user.FindAll("permission").Select(c => c.Value)
        })).RequireAuthorization();

        return app;
    }
}
