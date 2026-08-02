using ERP.Application.Common.Messaging;
using ERP.Application.Modules.Hr.Commands;
using ERP.Application.Modules.Hr.Queries;
using ERP.Domain.Modules.Users;
using ERP.Shared.Contracts.Hr;

namespace ERP.Api.Endpoints;

/// <summary>
/// İşçi (HR) API endpoint-ləri — versiyalı, RESTful (TDD §11). Nazik controller (TDD §16).
/// Oxu → hr.view, dəyişiklik → hr.edit (permission-based RBAC, TDD §7).
/// </summary>
public static class EmployeeEndpoints
{
    public static IEndpointRouteBuilder MapEmployeeEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/employees").WithTags("Employees");

        group.MapGet("/", async (string? search, ISender sender, int page = 1, int pageSize = 20) =>
        {
            var result = await sender.Send(new GetEmployeesQuery(search, page, pageSize));
            return Results.Ok(result);
        }).RequireAuthorization(Permissions.HrView);

        group.MapGet("/{id:guid}", async (Guid id, ISender sender) =>
        {
            var result = await sender.Send(new GetEmployeeByIdQuery(id));
            return result.IsSuccess ? Results.Ok(result.Value) : Results.NotFound(new { error = result.Error });
        }).RequireAuthorization(Permissions.HrView);

        group.MapPost("/", async (CreateEmployeeRequest request, ISender sender) =>
        {
            var result = await sender.Send(new CreateEmployeeCommand(request));
            return result.IsSuccess
                ? Results.Created($"/api/v1/employees/{result.Value}", new { id = result.Value })
                : Results.BadRequest(new { error = result.Error });
        }).RequireAuthorization(Permissions.HrEdit);

        group.MapPut("/{id:guid}", async (Guid id, UpdateEmployeeRequest request, ISender sender) =>
        {
            var result = await sender.Send(new UpdateEmployeeCommand(id, request));
            return result.IsSuccess ? Results.NoContent() : Results.BadRequest(new { error = result.Error });
        }).RequireAuthorization(Permissions.HrEdit);

        group.MapDelete("/{id:guid}", async (Guid id, ISender sender) =>
        {
            var result = await sender.Send(new ERP.Application.Modules.Hr.Commands.DeleteEmployeeCommand(id));
            return result.IsSuccess ? Results.NoContent() : Results.BadRequest(new { error = result.Error });
        }).RequireAuthorization(Permissions.HrEdit);

        return app;
    }
}
