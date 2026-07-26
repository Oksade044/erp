using ERP.Application.Common.Interfaces;
using ERP.Domain.Modules.Orders;
using ERP.Infrastructure.Persistence;
using ERP.Shared.Contracts.Products;
using Microsoft.EntityFrameworkCore;

namespace ERP.Infrastructure.Reports;

/// <summary>
/// Məhsulun anbarlar üzrə mövcudluğu (#18/#19/#27). Mövcud = StockLevel.Quantity;
/// Rezerv = təsdiqlənmiş sifariş sətirləri; Kirayə = təhvil verilmiş;
/// Boş = mövcud − rezerv − kirayə − təmir − zədəli.
/// </summary>
public sealed class AvailabilityReader(AppDbContext context) : IAvailabilityReader
{
    private static int Free(int total, int reserved, int rented, int inRepair, int damaged)
    {
        var free = total - reserved - rented - inRepair - damaged;
        return free < 0 ? 0 : free;
    }

    public async Task<IReadOnlyList<ProductAvailabilityDto>> GetProductAvailabilityAsync(
        Guid productId, CancellationToken ct = default)
    {
        var levels = await context.StockLevels
            .AsNoTracking()
            .Where(s => s.ProductId == productId)
            .Select(s => new { s.WarehouseId, s.WarehouseName, s.Quantity, s.InRepair, s.Damaged })
            .ToListAsync(ct);

        var lines = await ActiveLinesAsync([productId], ct);

        return levels
            .Select(lvl =>
            {
                var forWh = lines.Where(x => x.WarehouseId == lvl.WarehouseId).ToList();
                var reserved = forWh.Where(x => x.Status == OrderStatus.Təsdiqlənmiş).Sum(x => x.Quantity);
                var rented = forWh.Where(x => x.Status == OrderStatus.TəhvilVerilmiş).Sum(x => x.Quantity);
                return new ProductAvailabilityDto(
                    lvl.WarehouseId, lvl.WarehouseName, lvl.Quantity, reserved, rented,
                    Free(lvl.Quantity, reserved, rented, lvl.InRepair, lvl.Damaged),
                    lvl.InRepair, lvl.Damaged);
            })
            .OrderBy(a => a.WarehouseName)
            .ToList();
    }

    public async Task<IReadOnlyDictionary<Guid, StockSummaryDto>> GetSummariesAsync(
        IReadOnlyCollection<Guid> productIds, CancellationToken ct = default)
    {
        if (productIds.Count == 0)
            return new Dictionary<Guid, StockSummaryDto>();

        var levels = await context.StockLevels
            .AsNoTracking()
            .Where(s => productIds.Contains(s.ProductId))
            .Select(s => new { s.ProductId, s.Quantity, s.InRepair, s.Damaged })
            .ToListAsync(ct);

        var lines = await context.Set<OrderLine>()
            .AsNoTracking()
            .Where(l => productIds.Contains(l.ProductId))
            .Join(context.Orders, l => l.OrderId, o => o.Id,
                (l, o) => new { l.ProductId, l.Quantity, o.Status })
            .Where(x => x.Status == OrderStatus.Təsdiqlənmiş || x.Status == OrderStatus.TəhvilVerilmiş)
            .ToListAsync(ct);

        var result = new Dictionary<Guid, StockSummaryDto>();
        foreach (var pid in productIds.Distinct())
        {
            var lvl = levels.Where(x => x.ProductId == pid).ToList();
            var total = lvl.Sum(x => x.Quantity);
            var inRepair = lvl.Sum(x => x.InRepair);
            var damaged = lvl.Sum(x => x.Damaged);
            var reserved = lines.Where(x => x.ProductId == pid && x.Status == OrderStatus.Təsdiqlənmiş).Sum(x => x.Quantity);
            var rented = lines.Where(x => x.ProductId == pid && x.Status == OrderStatus.TəhvilVerilmiş).Sum(x => x.Quantity);
            result[pid] = new StockSummaryDto(total, reserved, rented, inRepair, damaged,
                Free(total, reserved, rented, inRepair, damaged));
        }
        return result;
    }

    private async Task<List<LineRow>> ActiveLinesAsync(IReadOnlyCollection<Guid> productIds, CancellationToken ct)
    {
        // EF pozitiv record ctor-unu SQL-də tərcümə edə bilmir → anonim proyeksiya + client map.
        var rows = await context.Set<OrderLine>()
            .AsNoTracking()
            .Where(l => productIds.Contains(l.ProductId) && l.WarehouseId != null)
            .Join(context.Orders, l => l.OrderId, o => o.Id,
                (l, o) => new { l.WarehouseId, l.Quantity, o.Status })
            .Where(x => x.Status == OrderStatus.Təsdiqlənmiş || x.Status == OrderStatus.TəhvilVerilmiş)
            .ToListAsync(ct);

        return rows.Select(x => new LineRow(x.WarehouseId!.Value, x.Quantity, x.Status)).ToList();
    }

    private sealed record LineRow(Guid WarehouseId, int Quantity, OrderStatus Status);
}
