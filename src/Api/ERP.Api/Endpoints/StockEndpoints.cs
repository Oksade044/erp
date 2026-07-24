using ERP.Application.Common.Messaging;
using ERP.Application.Modules.Warehouses.Commands;
using ERP.Application.Modules.Warehouses.Queries;
using ERP.Domain.Modules.Users;
using ERP.Shared.Contracts.Warehouses;

namespace ERP.Api.Endpoints;

/// <summary>
/// Stok (per-anbar səviyyələr, transfer, min-stok) API endpoint-ləri — RESTful (TDD §11).
/// Oxu → warehouses.view, dəyişiklik → warehouses.edit (RBAC, TDD §7).
/// </summary>
public static class StockEndpoints
{
    public static IEndpointRouteBuilder MapStockEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/stock").WithTags("Stock");

        group.MapGet("/levels", async (string? search, Guid? warehouseId, ISender sender,
            bool low = false, int page = 1, int pageSize = 20) =>
        {
            var result = await sender.Send(new GetStockLevelsQuery(search, warehouseId, low, page, pageSize));
            return Results.Ok(result);
        }).RequireAuthorization(Permissions.WarehousesView);

        // Minimum-stok xəbərdarlığı — həddin altındakı bütün səviyyələr.
        group.MapGet("/low", async (Guid? warehouseId, ISender sender, int page = 1, int pageSize = 50) =>
        {
            var result = await sender.Send(new GetStockLevelsQuery(null, warehouseId, LowOnly: true, page, pageSize));
            return Results.Ok(result);
        }).RequireAuthorization(Permissions.WarehousesView);

        group.MapPost("/adjust", async (AdjustStockRequest request, ISender sender) =>
        {
            var result = await sender.Send(new AdjustStockCommand(request));
            return result.IsSuccess
                ? Results.Ok(new { id = result.Value })
                : Results.BadRequest(new { error = result.Error });
        }).RequireAuthorization(Permissions.WarehousesEdit);

        group.MapPost("/transfer", async (TransferStockRequest request, ISender sender) =>
        {
            var result = await sender.Send(new TransferStockCommand(request));
            return result.IsSuccess ? Results.NoContent() : Results.BadRequest(new { error = result.Error });
        }).RequireAuthorization(Permissions.WarehousesEdit);

        return app;
    }
}
