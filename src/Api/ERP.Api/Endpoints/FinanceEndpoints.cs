using ERP.Application.Common.Messaging;
using ERP.Application.Modules.Finance.Commands;
using ERP.Application.Modules.Finance.Queries;
using ERP.Domain.Modules.Users;
using ERP.Shared.Contracts.Finance;

namespace ERP.Api.Endpoints;

/// <summary>
/// Maliyyə (kassa mədaxil/məxaric + pul axını) API endpoint-ləri — versiyalı, RESTful (TDD §11).
/// Nazik controller (TDD §16). Oxu → finance.view, dəyişiklik → finance.edit (RBAC, TDD §7).
/// </summary>
public static class FinanceEndpoints
{
    public static IEndpointRouteBuilder MapFinanceEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/finance").WithTags("Finance");

        group.MapGet("/transactions", async (string? search, string? type, ISender sender, int page = 1, int pageSize = 20) =>
        {
            var result = await sender.Send(new GetTransactionsQuery(search, type, page, pageSize));
            return Results.Ok(result);
        }).RequireAuthorization(Permissions.FinanceView);

        group.MapGet("/transactions/{id:guid}", async (Guid id, ISender sender) =>
        {
            var result = await sender.Send(new GetTransactionByIdQuery(id));
            return result.IsSuccess ? Results.Ok(result.Value) : Results.NotFound(new { error = result.Error });
        }).RequireAuthorization(Permissions.FinanceView);

        group.MapGet("/summary", async (ISender sender) =>
        {
            var result = await sender.Send(new GetCashFlowSummaryQuery());
            return Results.Ok(result);
        }).RequireAuthorization(Permissions.FinanceView);

        group.MapPost("/transactions", async (CreateTransactionRequest request, ISender sender) =>
        {
            var result = await sender.Send(new CreateTransactionCommand(request));
            return result.IsSuccess
                ? Results.Created($"/api/v1/finance/transactions/{result.Value}", new { id = result.Value })
                : Results.BadRequest(new { error = result.Error });
        }).RequireAuthorization(Permissions.FinanceEdit);

        return app;
    }
}
