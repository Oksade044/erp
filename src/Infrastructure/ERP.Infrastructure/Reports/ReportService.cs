using ERP.Application.Common.Interfaces;
using ERP.Domain.Modules.Invoices;
using ERP.Domain.Modules.Orders;
using ERP.Infrastructure.Persistence;
using ERP.Shared.Contracts.Reports;
using Microsoft.EntityFrameworkCore;

namespace ERP.Infrastructure.Reports;

/// <summary>
/// Hesabat aqreqasiyaları (TDD §5, §33). Saylar server tərəfdə; decimal cəmlər client tərəfdə
/// hesablanır, çünki SQLite decimal-ı TEXT kimi saxlayır və server-side SUM-u düzgün işləmir
/// (PostgreSQL-ə keçəndə bu, server-side SUM-a çevrilə bilər).
/// </summary>
public sealed class ReportService(AppDbContext context) : IReportService
{
    private const string DefaultCurrency = "AZN";

    public async Task<DashboardDto> GetDashboardAsync(CancellationToken ct = default)
    {
        var customerCount = await context.Customers.CountAsync(ct);
        var productCount = await context.Products.CountAsync(ct);
        var orderCount = await context.Orders.CountAsync(ct);

        var draft = await context.Orders.CountAsync(o => o.Status == OrderStatus.Qaralama, ct);
        var confirmed = await context.Orders.CountAsync(o => o.Status == OrderStatus.Təsdiqlənmiş, ct);
        var delivered = await context.Orders.CountAsync(o => o.Status == OrderStatus.TəhvilVerilmiş, ct);
        var returned = await context.Orders.CountAsync(o => o.Status == OrderStatus.Qaytarılmış, ct);
        var cancelled = await context.Orders.CountAsync(o => o.Status == OrderStatus.Ləğv, ct);

        // Pul cəmləri client tərəfdə (SQLite decimal aqreqasiyası etibarsızdır).
        var totalInvoiced = (await context.Invoices.Select(i => i.TotalAmount.Amount).ToListAsync(ct)).Sum();
        var totalPaid = (await context.Set<Payment>().Select(p => p.Amount.Amount).ToListAsync(ct)).Sum();

        return new DashboardDto(
            CustomerCount: customerCount,
            ProductCount: productCount,
            OrderCount: orderCount,
            DraftOrders: draft,
            ConfirmedOrders: confirmed,
            DeliveredOrders: delivered,
            ReturnedOrders: returned,
            CancelledOrders: cancelled,
            TotalInvoiced: totalInvoiced,
            TotalPaid: totalPaid,
            TotalOutstanding: totalInvoiced - totalPaid,
            Currency: DefaultCurrency);
    }

    public async Task<IReadOnlyList<OutstandingInvoiceDto>> GetOutstandingInvoicesAsync(CancellationToken ct = default)
    {
        var invoices = await context.Invoices.Include(i => i.Payments).ToListAsync(ct);

        return invoices
            .Select(i => new
            {
                Invoice = i,
                Paid = i.Payments.Sum(p => p.Amount.Amount)
            })
            .Where(x => x.Invoice.TotalAmount.Amount - x.Paid > 0)
            .OrderByDescending(x => x.Invoice.TotalAmount.Amount - x.Paid)
            .Select(x => new OutstandingInvoiceDto(
                InvoiceNumber: x.Invoice.InvoiceNumber,
                CustomerName: x.Invoice.CustomerName,
                Total: x.Invoice.TotalAmount.Amount,
                Paid: x.Paid,
                Balance: x.Invoice.TotalAmount.Amount - x.Paid,
                Currency: x.Invoice.TotalAmount.Currency,
                Status: x.Invoice.Status.ToString()))
            .ToList();
    }

    public async Task<IReadOnlyList<TopProductDto>> GetTopProductsAsync(int top, CancellationToken ct = default)
    {
        if (top <= 0) top = 10;

        // Anbarı rezerv edən/etmiş sifarişlərin sətirlərini yüklə, yaddaşda qrupla.
        var lines = await context.Orders
            .Where(o => o.Status == OrderStatus.Təsdiqlənmiş
                     || o.Status == OrderStatus.TəhvilVerilmiş
                     || o.Status == OrderStatus.Qaytarılmış)
            .SelectMany(o => o.Lines)
            .Select(l => new { l.ProductName, l.Quantity, l.OrderId })
            .ToListAsync(ct);

        return lines
            .GroupBy(l => l.ProductName)
            .Select(g => new TopProductDto(
                ProductName: g.Key,
                TotalQuantityRented: g.Sum(x => x.Quantity),
                OrderCount: g.Select(x => x.OrderId).Distinct().Count()))
            .OrderByDescending(x => x.TotalQuantityRented)
            .Take(top)
            .ToList();
    }
}
