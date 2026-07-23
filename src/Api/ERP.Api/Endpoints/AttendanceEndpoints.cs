using ERP.Application.Common.Messaging;
using ERP.Application.Modules.Hr.Commands;
using ERP.Application.Modules.Hr.Queries;
using ERP.Domain.Modules.Users;
using ERP.Shared.Contracts.Hr;

namespace ERP.Api.Endpoints;

/// <summary>
/// Davamiyyət (Attendance) API endpoint-ləri — versiyalı, RESTful (TDD §11). Nazik controller.
/// HR bloku olduğu üçün hr.view/hr.edit icazələrini istifadə edir (TDD §7).
/// </summary>
public static class AttendanceEndpoints
{
    public static IEndpointRouteBuilder MapAttendanceEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/attendance").WithTags("Attendance");

        group.MapGet("/", async (string? search, Guid? employeeId, ISender sender, int page = 1, int pageSize = 20) =>
        {
            var result = await sender.Send(new GetAttendanceQuery(search, employeeId, page, pageSize));
            return Results.Ok(result);
        }).RequireAuthorization(Permissions.HrView);

        group.MapPost("/", async (CreateAttendanceRequest request, ISender sender) =>
        {
            var result = await sender.Send(new CreateAttendanceCommand(request));
            return result.IsSuccess
                ? Results.Created($"/api/v1/attendance/{result.Value}", new { id = result.Value })
                : Results.BadRequest(new { error = result.Error });
        }).RequireAuthorization(Permissions.HrEdit);

        return app;
    }
}
