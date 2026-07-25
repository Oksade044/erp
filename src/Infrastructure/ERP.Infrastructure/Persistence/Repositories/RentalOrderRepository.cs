using ERP.Application.Common.Interfaces;
using ERP.Application.Common.Models;
using ERP.Domain.Modules.Orders;
using Microsoft.EntityFrameworkCore;

namespace ERP.Infrastructure.Persistence.Repositories;

/// <summary>İcarə sifarişinə xas repository implementasiyası (TDD §14, §38).</summary>
public sealed class RentalOrderRepository(AppDbContext context)
    : Repository<RentalOrder>(context), IRentalOrderRepository
{
    public async Task<RentalOrder?> GetByIdWithLinesAsync(Guid id, CancellationToken ct = default) =>
        await Set.Include(o => o.Lines).FirstOrDefaultAsync(o => o.Id == id, ct);

    public async Task<PagedResult<RentalOrder>> SearchAsync(
        string? search, int page, int pageSize, CancellationToken ct = default)
    {
        var query = Set.AsNoTracking().Include(o => o.Lines);

        // Axtarışla: diakritiksiz, sıralanmış (müştəri adı və ya sifariş nömrəsi üzrə).
        if (!string.IsNullOrWhiteSpace(search))
        {
            var all = await query.ToListAsync(ct);
            return RankedSearch.Page(all, search, page, pageSize,
                primary: o => o.CustomerName,
                secondary: o => [o.OrderNumber]);
        }

        // Axtarışsız: ən yeni öndə. OrderNumber tarixlə başlayır (ORD-yyyyMMdd-...) → provider-safe.
        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(o => o.OrderNumber)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return new PagedResult<RentalOrder>
        {
            Items = items,
            TotalCount = total,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<int> GetReservedQuantityAsync(
        Guid productId, DateOnly startDate, DateOnly endDate, Guid excludeOrderId,
        CancellationToken ct = default)
    {
        // Anbarı rezerv edən (təsdiqlənmiş/təhvil verilmiş) və tarix aralığı üst-üstə düşən
        // digər sifarişlərdəki bu məhsulun ümumi sayı. Üst-üstə düşmə: start ≤ o.End AND end ≥ o.Start.
        var query =
            from o in Set
            where o.Id != excludeOrderId
                  && (o.Status == OrderStatus.Təsdiqlənmiş || o.Status == OrderStatus.TəhvilVerilmiş)
                  && o.StartDate <= endDate && o.EndDate >= startDate
            from l in o.Lines
            where l.ProductId == productId
            select l.Quantity;

        return await query.SumAsync(ct);
    }
}
