using ERP.Application.Modules.Products.Commands;
using ERP.Application.Modules.Products.Queries;
using ERP.Shared.Contracts.Products;
using ERP.Application.Common.Messaging;

namespace ERP.Api.Endpoints;

/// <summary>
/// Məhsul API endpoint-ləri — versiyalı, RESTful (TDD §11). Nazik controller: MediatR-a ötürür.
/// </summary>
public static class ProductEndpoints
{
    public static IEndpointRouteBuilder MapProductEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/products").WithTags("Products");

        group.MapGet("/", async (string? search, ISender sender, int page = 1, int pageSize = 20) =>
        {
            var result = await sender.Send(new GetProductsQuery(search, page, pageSize));
            return Results.Ok(result);
        });

        group.MapGet("/{id:guid}", async (Guid id, ISender sender) =>
        {
            var result = await sender.Send(new GetProductByIdQuery(id));
            return result.IsSuccess ? Results.Ok(result.Value) : Results.NotFound(new { error = result.Error });
        });

        group.MapPost("/", async (CreateProductRequest request, ISender sender) =>
        {
            var result = await sender.Send(new CreateProductCommand(request));
            return result.IsSuccess
                ? Results.Created($"/api/v1/products/{result.Value}", new { id = result.Value })
                : Results.BadRequest(new { error = result.Error });
        });

        group.MapPut("/{id:guid}", async (Guid id, UpdateProductRequest request, ISender sender) =>
        {
            var result = await sender.Send(new UpdateProductCommand(id, request));
            return result.IsSuccess ? Results.NoContent() : Results.BadRequest(new { error = result.Error });
        });

        return app;
    }
}
