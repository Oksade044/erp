using ERP.Application.Common.Interfaces;
using ERP.Domain.Modules.Orders;
using ERP.Infrastructure.Persistence;
using ERP.Shared.Contracts.Products;
using Microsoft.EntityFrameworkCore;

namespace ERP.Infrastructure.Reports;

/// <summary>
/// Məhsulun anbarlar üzrə mövcudluğu (#18/#19). Mövcud = StockLevel.Quantity;
/// Rezerv = təsdiqlənmiş sifariş sətirləri; Kirayə = təhvil verilmiş; Boş = mövcud−rezerv−kirayə.
/// </summary>
public sealed class AvailabilityReader(AppDbContext context) : IAvailabilityReader
{
    public async Task<IReadOnlyList<ProductAvailabilityDto>> GetProductAvailabilityAsync(
        Guid productId, CancellationToken ct = default)
    {
        var levels = await context.StockLevels
            .AsNoTracking()
            .Where(s => s.ProductId == productId)
            .Select(s => new { s.WarehouseId, s.WarehouseName, s.Quantity })
            .ToListAsync(ct);

        // Bu məhsulun anbar təyin edilmiş sətirləri (aktiv statuslu sifarişlərdə).
        var lines = await context.Set<OrderLine>()
            .AsNoTracking()
            .Where(l => l.ProductId == productId && l.WarehouseId != null)
            .Join(context.Orders, l => l.OrderId, o => o.Id,
                (l, o) => new { WarehouseId = l.WarehouseId!.Value, l.Quantity, o.Status })
            .Where(x => x.Status == OrderStatus.Təsdiqlənmiş || x.Status == OrderStatus.TəhvilVerilmiş)
            .ToListAsync(ct);

        return levels
            .Select(lvl =>
            {
                var forWh = lines.Where(x => x.WarehouseId == lvl.WarehouseId).ToList();
                var reserved = forWh.Where(x => x.Status == OrderStatus.Təsdiqlənmiş).Sum(x => x.Quantity);
                var rented = forWh.Where(x => x.Status == OrderStatus.TəhvilVerilmiş).Sum(x => x.Quantity);
                var free = lvl.Quantity - reserved - rented;
                return new ProductAvailabilityDto(
                    lvl.WarehouseId, lvl.WarehouseName, lvl.Quantity, reserved, rented, free < 0 ? 0 : free);
            })
            .OrderBy(a => a.WarehouseName)
            .ToList();
    }
}
