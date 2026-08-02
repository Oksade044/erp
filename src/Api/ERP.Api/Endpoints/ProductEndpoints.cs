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

        // --- Kateqoriyalar (məhsul formasında seçim üçün) ---
        var categories = app.MapGroup("/api/v1/categories").WithTags("Categories");

        categories.MapGet("/", async (ISender sender) =>
            Results.Ok(await sender.Send(new GetCategoriesQuery())));

        categories.MapPost("/", async (CreateCategoryRequest request, ISender sender) =>
        {
            var result = await sender.Send(new CreateCategoryCommand(request));
            return result.IsSuccess
                ? Results.Created($"/api/v1/categories/{result.Value}", new { id = result.Value })
                : Results.BadRequest(new { error = result.Error });
        })
        .RequireAuthorization(ERP.Domain.Modules.Users.Permissions.ProductsEdit);

        categories.MapPut("/{id:guid}", async (Guid id, CreateCategoryRequest request, ISender sender) =>
        {
            var result = await sender.Send(new UpdateCategoryCommand(id, request.Name));
            return result.IsSuccess ? Results.NoContent() : Results.BadRequest(new { error = result.Error });
        }).RequireAuthorization(ERP.Domain.Modules.Users.Permissions.ProductsEdit);

        categories.MapDelete("/{id:guid}", async (Guid id, ISender sender) =>
        {
            var result = await sender.Send(new DeleteCategoryCommand(id));
            return result.IsSuccess ? Results.NoContent() : Results.BadRequest(new { error = result.Error });
        }).RequireAuthorization(ERP.Domain.Modules.Users.Permissions.ProductsEdit);

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

        // #17 — məhsulun anbarlar üzrə stok səviyyələri (redaktədə göstərmək üçün).
        group.MapGet("/{id:guid}/stock", async (Guid id, ISender sender) =>
            Results.Ok(await sender.Send(new ERP.Application.Modules.Warehouses.Queries.GetProductStockQuery(id))));

        // #18/#19 — məhsulun anbarlar üzrə mövcudluğu (rezerv/kirayə/boş).
        group.MapGet("/{id:guid}/availability", async (Guid id, ISender sender) =>
            Results.Ok(await sender.Send(new GetProductAvailabilityQuery(id))));

        // #38 — məhsulun istifadə tarixçəsi.
        group.MapGet("/{id:guid}/history", async (Guid id, ISender sender) =>
            Results.Ok(await sender.Send(new GetProductHistoryQuery(id))));

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

        group.MapDelete("/{id:guid}", async (Guid id, ISender sender) =>
        {
            var result = await sender.Send(new DeleteProductCommand(id));
            return result.IsSuccess ? Results.NoContent() : Results.BadRequest(new { error = result.Error });
        }).RequireAuthorization(ERP.Domain.Modules.Users.Permissions.ProductsEdit);

        // QR kod (TDD §27) — məhsulun SKU-su PNG QR kimi (anbar skanlaması üçün).
        group.MapGet("/{id:guid}/qr", async (Guid id, ISender sender) =>
        {
            var result = await sender.Send(new GetProductQrQuery(id));
            return result.IsSuccess
                ? Results.File(result.Value!, "image/png")
                : Results.NotFound(new { error = result.Error });
        });

        // Excel ixrac (TDD §26) — bütün məhsullar xlsx kimi.
        group.MapGet("/export", async (ISender sender) =>
        {
            var bytes = await sender.Send(new ExportProductsQuery());
            return Results.File(bytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"mehsullar-{DateTime.Now:yyyyMMdd}.xlsx");
        });

        // Excel idxal (TDD §26) — yalnız products.edit icazəsi.
        group.MapPost("/import", async (IFormFile file, ISender sender) =>
        {
            using var ms = new MemoryStream();
            await file.CopyToAsync(ms);
            var result = await sender.Send(new ImportProductsCommand(ms.ToArray()));
            return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(new { error = result.Error });
        })
        .RequireAuthorization(ERP.Domain.Modules.Users.Permissions.ProductsEdit)
        .DisableAntiforgery();

        // Şəkil yükləmə (TDD §23/§24) — fayl storage-ə yazılır, hamıya API ilə görünür.
        group.MapPost("/{id:guid}/image", async (Guid id, IFormFile file, ISender sender) =>
        {
            using var ms = new MemoryStream();
            await file.CopyToAsync(ms);
            var ext = Path.GetExtension(file.FileName);
            var result = await sender.Send(new SetProductImageCommand(id, ms.ToArray(), ext));
            return result.IsSuccess ? Results.Ok(new { key = result.Value }) : Results.BadRequest(new { error = result.Error });
        })
        .RequireAuthorization(ERP.Domain.Modules.Users.Permissions.ProductsEdit)
        .DisableAntiforgery();

        // Şəkil oxuma — istənilən istifadəçi (products.view) API-dən şəkli alır.
        group.MapGet("/{id:guid}/image", async (Guid id, ISender sender) =>
        {
            var result = await sender.Send(new GetProductImageQuery(id));
            return result.IsSuccess
                ? Results.File(result.Value!.Content, result.Value.ContentType)
                : Results.NotFound(new { error = result.Error });
        });

        return app;
    }
}
