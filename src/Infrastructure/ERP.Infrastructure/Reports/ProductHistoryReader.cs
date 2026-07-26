using ERP.Application.Common.Interfaces;
using ERP.Domain.Modules.Orders;
using ERP.Infrastructure.Persistence;
using ERP.Shared.Contracts.Products;
using Microsoft.EntityFrameworkCore;

namespace ERP.Infrastructure.Reports;

/// <summary>Məhsulun istifadə tarixçəsi (#38) — sifariş sətirlərindən.</summary>
public sealed class ProductHistoryReader(AppDbContext context) : IProductHistoryReader
{
    public async Task<IReadOnlyList<ProductHistoryRowDto>> GetAsync(Guid productId, CancellationToken ct = default)
    {
        // Bu məhsulun bütün sifariş sətirləri + sifariş məlumatı (silinmiş məhsul üçün də qalır).
        var rows = await context.Set<OrderLine>()
            .IgnoreQueryFilters()
            .Where(l => l.ProductId == productId)
            .Join(context.Orders.IgnoreQueryFilters(), l => l.OrderId, o => o.Id, (l, o) => new
            {
                o.OrderNumber, o.CustomerName, o.CreatedByName, o.OrderType, o.Status,
                l.Quantity, Price = l.UnitPrice.Amount, Cur = l.UnitPrice.Currency,
                o.StartDate, o.EndDate, l.WarehouseName
            })
            .ToListAsync(ct);

        // Faktura nömrələri (sifariş nömrəsinə görə).
        var orderNumbers = rows.Select(r => r.OrderNumber).Distinct().ToList();
        var invoices = await context.Invoices.IgnoreQueryFilters()
            .Where(i => orderNumbers.Contains(i.OrderNumber))
            .Select(i => new { i.OrderNumber, i.InvoiceNumber })
            .ToListAsync(ct);
        var invByOrder = invoices
            .GroupBy(i => i.OrderNumber)
            .ToDictionary(g => g.Key, g => g.First().InvoiceNumber);

        return rows
            .OrderByDescending(r => r.OrderNumber)
            .Select(r => new ProductHistoryRowDto(
                OrderNumber: r.OrderNumber,
                InvoiceNumber: invByOrder.TryGetValue(r.OrderNumber, out var inv) ? inv : null,
                CustomerName: r.CustomerName,
                EmployeeName: r.CreatedByName,
                OrderType: r.OrderType == OrderType.Satış ? "Satış" : "İcarə",
                Status: r.Status.ToString(),
                Quantity: r.Quantity,
                UnitPrice: r.Price,
                Currency: r.Cur,
                StartDate: r.StartDate,
                EndDate: r.EndDate,
                Days: r.EndDate.DayNumber - r.StartDate.DayNumber,
                WarehouseName: r.WarehouseName))
            .ToList();
    }
}
