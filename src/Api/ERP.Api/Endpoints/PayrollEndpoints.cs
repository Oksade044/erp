using ERP.Application.Common.Messaging;
using ERP.Application.Modules.Hr.Commands;
using ERP.Application.Modules.Hr.Queries;
using ERP.Domain.Modules.Users;
using ERP.Shared.Contracts.Hr;

namespace ERP.Api.Endpoints;

/// <summary>
/// Əməkhaqqı (Payroll) API endpoint-ləri — versiyalı, RESTful (TDD §11). Nazik controller.
/// HR bloku → hr.view/hr.edit icazələri (TDD §7). Ödəniş Maliyyəyə məxaric yaradır.
/// </summary>
public static class PayrollEndpoints
{
    public static IEndpointRouteBuilder MapPayrollEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/payrolls").WithTags("Payrolls");

        group.MapGet("/", async (string? search, Guid? employeeId, ISender sender, int page = 1, int pageSize = 20) =>
        {
            var result = await sender.Send(new GetPayrollsQuery(search, employeeId, page, pageSize));
            return Results.Ok(result);
        }).RequireAuthorization(Permissions.HrView);

        group.MapGet("/{id:guid}", async (Guid id, ISender sender) =>
        {
            var result = await sender.Send(new GetPayrollByIdQuery(id));
            return result.IsSuccess ? Results.Ok(result.Value) : Results.NotFound(new { error = result.Error });
        }).RequireAuthorization(Permissions.HrView);

        group.MapPost("/", async (CreatePayrollRequest request, ISender sender) =>
        {
            var result = await sender.Send(new CreatePayrollCommand(request));
            return result.IsSuccess
                ? Results.Created($"/api/v1/payrolls/{result.Value}", new { id = result.Value })
                : Results.BadRequest(new { error = result.Error });
        }).RequireAuthorization(Permissions.HrEdit);

        group.MapPost("/{id:guid}/pay", async (Guid id, ISender sender) =>
        {
            var result = await sender.Send(new PayPayrollCommand(id));
            return result.IsSuccess ? Results.NoContent() : Results.BadRequest(new { error = result.Error });
        }).RequireAuthorization(Permissions.HrEdit);

        // Hissə-hissə ödəniş (installment).
        group.MapPost("/{id:guid}/payments", async (Guid id, AddPayrollPaymentRequest request, ISender sender) =>
        {
            var result = await sender.Send(new AddPayrollPaymentCommand(id, request.Amount, request.Date, request.Method, request.Note));
            return result.IsSuccess ? Results.NoContent() : Results.BadRequest(new { error = result.Error });
        }).RequireAuthorization(Permissions.HrEdit);

        // Aylıq bonus.
        group.MapPost("/{id:guid}/bonus", async (Guid id, AddPayrollPaymentRequest request, ISender sender) =>
        {
            var result = await sender.Send(new AddPayrollBonusCommand(id, request.Amount, request.Date, request.Method, request.Note));
            return result.IsSuccess ? Results.NoContent() : Results.BadRequest(new { error = result.Error });
        }).RequireAuthorization(Permissions.HrEdit);

        return app;
    }
}
