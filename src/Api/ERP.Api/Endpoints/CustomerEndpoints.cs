using ERP.Application.Modules.Customers.Commands;
using ERP.Application.Modules.Customers.Queries;
using ERP.Shared.Contracts.Customers;
using ERP.Application.Common.Messaging;

namespace ERP.Api.Endpoints;

/// <summary>
/// Müştəri API endpoint-ləri — versiyalı, RESTful (TDD §11). Controller-lər nazikdir:
/// yalnız MediatR-a ötürür (TDD §16, §17).
/// </summary>
public static class CustomerEndpoints
{
    public static IEndpointRouteBuilder MapCustomerEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/customers").WithTags("Customers");

        group.MapGet("/", async (string? search, ISender sender, int page = 1, int pageSize = 20) =>
        {
            var result = await sender.Send(new GetCustomersQuery(search, page, pageSize));
            return Results.Ok(result);
        });

        group.MapGet("/{id:guid}", async (Guid id, ISender sender) =>
        {
            var result = await sender.Send(new GetCustomerByIdQuery(id));
            return result.IsSuccess ? Results.Ok(result.Value) : Results.NotFound(new { error = result.Error });
        });

        group.MapPost("/", async (CreateCustomerRequest request, ISender sender) =>
        {
            var result = await sender.Send(new CreateCustomerCommand(request));
            return result.IsSuccess
                ? Results.Created($"/api/v1/customers/{result.Value}", new { id = result.Value })
                : Results.BadRequest(new { error = result.Error });
        });

        group.MapPut("/{id:guid}", async (Guid id, UpdateCustomerRequest request, ISender sender) =>
        {
            var result = await sender.Send(new UpdateCustomerCommand(id, request));
            return result.IsSuccess ? Results.NoContent() : Results.BadRequest(new { error = result.Error });
        });

        return app;
    }
}
