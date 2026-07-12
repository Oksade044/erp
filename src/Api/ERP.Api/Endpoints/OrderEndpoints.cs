using ERP.Application.Modules.Orders.Commands;
using ERP.Application.Modules.Orders.Queries;
using ERP.Shared.Contracts.Orders;
using MediatR;

namespace ERP.Api.Endpoints;

/// <summary>
/// İcarə sifarişi endpoint-ləri — versiyalı, RESTful (TDD §11). Nazik controller: MediatR-a ötürür.
/// Status keçidləri sub-resurs action kimi (confirm/cancel).
/// </summary>
public static class OrderEndpoints
{
    public static IEndpointRouteBuilder MapOrderEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/orders").WithTags("Orders");

        group.MapGet("/", async (string? search, ISender sender, int page = 1, int pageSize = 20) =>
        {
            var result = await sender.Send(new GetOrdersQuery(search, page, pageSize));
            return Results.Ok(result);
        });

        group.MapGet("/{id:guid}", async (Guid id, ISender sender) =>
        {
            var result = await sender.Send(new GetOrderByIdQuery(id));
            return result.IsSuccess ? Results.Ok(result.Value) : Results.NotFound(new { error = result.Error });
        });

        group.MapPost("/", async (CreateOrderRequest request, ISender sender) =>
        {
            var result = await sender.Send(new CreateOrderCommand(request));
            return result.IsSuccess
                ? Results.Created($"/api/v1/orders/{result.Value}", new { id = result.Value })
                : Results.BadRequest(new { error = result.Error });
        });

        group.MapPost("/{id:guid}/confirm", async (Guid id, ISender sender) =>
        {
            var result = await sender.Send(new ConfirmOrderCommand(id));
            return result.IsSuccess ? Results.NoContent() : Results.BadRequest(new { error = result.Error });
        });

        group.MapPost("/{id:guid}/cancel", async (Guid id, ISender sender) =>
        {
            var result = await sender.Send(new CancelOrderCommand(id));
            return result.IsSuccess ? Results.NoContent() : Results.BadRequest(new { error = result.Error });
        });

        return app;
    }
}
