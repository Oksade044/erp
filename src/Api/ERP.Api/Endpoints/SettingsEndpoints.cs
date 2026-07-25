using ERP.Application.Common.Messaging;
using ERP.Application.Modules.Settings.Commands;
using ERP.Application.Modules.Settings.Queries;
using ERP.Domain.Modules.Users;
using ERP.Shared.Contracts.Settings;

namespace ERP.Api.Endpoints;

/// <summary>
/// Parametrlər — sahə-səviyyəli görünürlük (TDD §7). Oxumaq hər autentifikasiya olunmuş
/// istifadəçiyə açıqdır (klient öz görünürlüyünü bilməlidir); dəyişmək yalnız users.manage.
/// </summary>
public static class SettingsEndpoints
{
    public static IEndpointRouteBuilder MapSettingsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/settings").WithTags("Settings");

        group.MapGet("/field-permissions", async (ISender sender) =>
            Results.Ok(await sender.Send(new GetFieldPermissionsQuery())));

        group.MapPut("/field-permissions", async (UpdateFieldPermissionRequest request, ISender sender) =>
        {
            var result = await sender.Send(new UpdateFieldPermissionCommand(request));
            return result.IsSuccess ? Results.NoContent() : Results.BadRequest(new { error = result.Error });
        })
        .RequireAuthorization(Permissions.UsersManage);

        return app;
    }
}
