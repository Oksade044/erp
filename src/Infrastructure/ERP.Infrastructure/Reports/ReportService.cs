using ERP.Application.Common.Interfaces;
using ERP.Domain.Modules.Finance;
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
        // #5 — müştərilərin bizə olan borcu da "alınacaqlar"a daxildir.
        var customerDebt = (await context.Customers.Where(c => c.Debt != null).Select(c => c.Debt!.Amount).ToListAsync(ct)).Sum();

        // #20 — bugünkü əməliyyatlar (DateOnly SQLite-də WHERE-də təhlükəsizdir).
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var deliveriesToday = await context.Orders.CountAsync(
            o => o.StartDate == today && o.Status == OrderStatus.Təsdiqlənmiş, ct);
        var returnsToday = await context.Orders.CountAsync(
            o => o.EndDate == today && o.Status == OrderStatus.TəhvilVerilmiş, ct);
        var overdueReturns = await context.Orders.CountAsync(
            o => o.EndDate < today && o.Status == OrderStatus.TəhvilVerilmiş, ct);

        // #23 — ödəniş tarixinə görə gəlir (client tərəfdə decimal cəm).
        var payments = await context.Set<Payment>()
            .Select(p => new { p.PaidAt, Amount = p.Amount.Amount })
            .ToListAsync(ct);
        var startOfWeek = today.AddDays(-(((int)today.DayOfWeek + 6) % 7)); // həftə bazar ertəsindən
        var startOfMonth = new DateOnly(today.Year, today.Month, 1);
        var startOfYear = new DateOnly(today.Year, 1, 1);

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
            TotalOutstanding: (totalInvoiced - totalPaid) + customerDebt,
            Currency: DefaultCurrency,
            DeliveriesToday: deliveriesToday,
            ReturnsToday: returnsToday,
            OverdueReturns: overdueReturns,
            IncomeToday: payments.Where(p => p.PaidAt == today).Sum(p => p.Amount),
            IncomeThisWeek: payments.Where(p => p.PaidAt >= startOfWeek).Sum(p => p.Amount),
            IncomeThisMonth: payments.Where(p => p.PaidAt >= startOfMonth).Sum(p => p.Amount),
            IncomeThisYear: payments.Where(p => p.PaidAt >= startOfYear).Sum(p => p.Amount));
    }

    public async Task<IReadOnlyList<OutstandingInvoiceDto>> GetOutstandingInvoicesAsync(CancellationToken ct = default)
    {
        var invoices = await context.Invoices.Include(i => i.Payments).ToListAsync(ct);

        return invoices
            .Select(i => new
            {
                Invoice = i,
                Paid = i.Payments.Sum(p => p.Amount.Amount),
                Grand = i.GrandTotal.Amount
            })
            .Where(x => x.Grand - x.Paid > 0)
            .OrderByDescending(x => x.Grand - x.Paid)
            .Select(x => new OutstandingInvoiceDto(
                InvoiceNumber: x.Invoice.InvoiceNumber,
                CustomerName: x.Invoice.CustomerName,
                Total: x.Grand,
                Paid: x.Paid,
                Balance: x.Grand - x.Paid,
                Currency: x.Invoice.TotalAmount.Currency,
                Status: x.Invoice.Status.ToString(),
                InvoiceId: x.Invoice.Id,
                OrderNumber: x.Invoice.OrderNumber))
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

    public async Task<ProfitLossDto> GetProfitLossAsync(DateOnly from, DateOnly to, CancellationToken ct = default)
    {
        // Dövr üzrə maliyyə əməliyyatları; pul cəmləri client tərəfdə (SQLite decimal gotcha).
        var rows = await context.FinancialTransactions
            .Where(t => t.Date >= from && t.Date <= to)
            .Select(t => new { t.Type, t.Category, Amount = t.Amount.Amount })
            .ToListAsync(ct);

        var income = rows.Where(r => r.Type == TransactionType.Mədaxil).Sum(r => r.Amount);
        var expense = rows.Where(r => r.Type == TransactionType.Məxaric).Sum(r => r.Amount);

        var incomeByCat = rows.Where(r => r.Type == TransactionType.Mədaxil)
            .GroupBy(r => r.Category)
            .Select(g => new CategoryAmountDto(g.Key, g.Sum(x => x.Amount)))
            .OrderByDescending(c => c.Amount).ToList();

        var expenseByCat = rows.Where(r => r.Type == TransactionType.Məxaric)
            .GroupBy(r => r.Category)
            .Select(g => new CategoryAmountDto(g.Key, g.Sum(x => x.Amount)))
            .OrderByDescending(c => c.Amount).ToList();

        return new ProfitLossDto(
            TotalIncome: income,
            TotalExpense: expense,
            NetProfit: income - expense,
            Currency: DefaultCurrency,
            IncomeByCategory: incomeByCat,
            ExpenseByCategory: expenseByCat);
    }

    public async Task<MonthlyRevenueDto> GetMonthlyRevenueAsync(int year, CancellationToken ct = default)
    {
        var rows = await context.FinancialTransactions
            .Where(t => t.Date.Year == year)
            .Select(t => new { t.Type, t.Date.Month, Amount = t.Amount.Amount })
            .ToListAsync(ct);

        var points = Enumerable.Range(1, 12).Select(m =>
        {
            var inc = rows.Where(r => r.Month == m && r.Type == TransactionType.Mədaxil).Sum(r => r.Amount);
            var exp = rows.Where(r => r.Month == m && r.Type == TransactionType.Məxaric).Sum(r => r.Amount);
            return new MonthlyPointDto(m, inc, exp, inc - exp);
        }).ToList();

        return new MonthlyRevenueDto(year, DefaultCurrency, points);
    }

    public async Task<IReadOnlyList<EmployeePerformanceRowDto>> GetEmployeePerformanceAsync(
        DateOnly from, DateOnly to, CancellationToken ct = default)
    {
        var fromUtc = from.ToDateTime(TimeOnly.MinValue);
        var toUtc = to.ToDateTime(TimeOnly.MaxValue);

        // Sifarişləri sətirlərlə gətir (dövriyyə = sətirlərin cəmi; decimal client tərəfdə).
        // Ləğv olunmuş sifarişlər sayılmır.
        var orders = (await context.Orders
                .Where(o => o.Status != OrderStatus.Ləğv)
                .Select(o => new
                {
                    o.CreatedByName,
                    o.CreatedByRole,
                    o.CreatedAt,
                    Lines = o.Lines.Select(l => new { l.Quantity, Amount = l.UnitPrice.Amount })
                })
                .ToListAsync(ct))
            .Where(o => o.CreatedByName != null
                        && o.CreatedAt.UtcDateTime >= fromUtc
                        && o.CreatedAt.UtcDateTime <= toUtc)
            .ToList();

        return orders
            .GroupBy(o => o.CreatedByName!)
            .Select(g => new EmployeePerformanceRowDto(
                EmployeeName: g.Key,
                Role: g.Select(x => x.CreatedByRole).FirstOrDefault(r => r != null),
                OrderCount: g.Count(),
                TotalRevenue: g.Sum(o => o.Lines.Sum(l => l.Amount * l.Quantity)),
                Currency: DefaultCurrency))
            .OrderByDescending(r => r.TotalRevenue)
            .ToList();
    }

    public async Task<IReadOnlyList<CustomerReportRowDto>> GetCustomerReportAsync(CancellationToken ct = default)
    {
        // Sifariş sayı müştəri üzrə.
        var orderCounts = (await context.Orders
                .Select(o => o.CustomerId)
                .ToListAsync(ct))
            .GroupBy(id => id)
            .ToDictionary(g => g.Key, g => g.Count());

        // Faktura + ödəniş məbləğləri müştəri üzrə (client tərəfdə decimal cəm).
        var invoices = await context.Invoices.Include(i => i.Payments)
            .Select(i => new
            {
                i.CustomerId,
                i.CustomerName,
                Total = i.TotalAmount.Amount,
                Paid = i.Payments.Sum(p => p.Amount.Amount)
            })
            .ToListAsync(ct);

        return invoices
            .GroupBy(i => new { i.CustomerId, i.CustomerName })
            .Select(g =>
            {
                var invoiced = g.Sum(x => x.Total);
                var paid = g.Sum(x => x.Paid);
                return new CustomerReportRowDto(
                    CustomerId: g.Key.CustomerId,
                    CustomerName: g.Key.CustomerName,
                    OrderCount: orderCounts.GetValueOrDefault(g.Key.CustomerId, 0),
                    TotalInvoiced: invoiced,
                    TotalPaid: paid,
                    Outstanding: invoiced - paid,
                    Currency: DefaultCurrency);
            })
            .OrderByDescending(r => r.TotalInvoiced)
            .ToList();
    }

    public async Task<IReadOnlyList<DamageReportRowDto>> GetDamageReportAsync(CancellationToken ct = default)
    {
        var orders = await context.Orders
            .Where(o => o.IsSettled)
            .Select(o => new
            {
                o.OrderNumber,
                o.CustomerName,
                Damage = o.DamageCharge != null ? o.DamageCharge.Amount : 0m,
                Penalty = o.PenaltyCharge != null ? o.PenaltyCharge.Amount : 0m,
                o.SettlementNotes
            })
            .ToListAsync(ct);

        return orders
            .Where(o => o.Damage > 0 || o.Penalty > 0)
            .Select(o => new DamageReportRowDto(
                OrderNumber: o.OrderNumber,
                CustomerName: o.CustomerName,
                DamageCharge: o.Damage,
                PenaltyCharge: o.Penalty,
                TotalCharges: o.Damage + o.Penalty,
                Currency: DefaultCurrency,
                SettlementNotes: o.SettlementNotes))
            .OrderByDescending(r => r.TotalCharges)
            .ToList();
    }

    public async Task<IReadOnlyList<RentalCalendarEntryDto>> GetRentalCalendarAsync(
        DateOnly from, DateOnly to, CancellationToken ct = default)
    {
        // Yalnız icarə (satış deyil) və ləğv olunmamış; dövrlə kəsişən sifarişlər.
        // Kəsişmə: sifariş [Start,End] ∩ [from,to] ≠ ∅  →  Start <= to && End >= from.
        var orders = await context.Orders
            .Where(o => o.OrderType == OrderType.İcarə
                        && o.Status != OrderStatus.Ləğv
                        && o.StartDate <= to && o.EndDate >= from)
            .Select(o => new
            {
                o.Id,
                o.OrderNumber,
                o.CustomerName,
                o.StartDate,
                o.EndDate,
                o.Status,
                Lines = o.Lines.Select(l => new { l.Quantity, Amount = l.UnitPrice.Amount })
            })
            .ToListAsync(ct);

        return orders
            .Select(o => new RentalCalendarEntryDto(
                OrderId: o.Id,
                OrderNumber: o.OrderNumber,
                CustomerName: o.CustomerName,
                StartDate: o.StartDate,
                EndDate: o.EndDate,
                Status: o.Status.ToString(),
                Total: o.Lines.Sum(l => l.Amount * l.Quantity),
                Currency: DefaultCurrency,
                DeliversInRange: o.StartDate >= from && o.StartDate <= to,
                ReturnsInRange: o.EndDate >= from && o.EndDate <= to))
            .OrderBy(r => r.StartDate)
            .ThenBy(r => r.OrderNumber)
            .ToList();
    }

    public async Task<ERP.Shared.Contracts.Mobile.EmployeeDashboardDto> GetEmployeeDashboardAsync(
        string employeeName, CancellationToken ct = default)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var monthStart = new DateOnly(today.Year, today.Month, 1);

        // Yalnız bu işçinin yaratdığı sifarişlər (CreatedByName snapshot).
        var orders = (await context.Orders
                .Where(o => o.CreatedByName == employeeName)
                .Select(o => new
                {
                    o.Status,
                    o.StartDate,
                    o.EndDate,
                    o.CreatedAt,
                    Total = o.Lines.Sum(l => l.UnitPrice.Amount * l.Quantity)
                })
                .ToListAsync(ct));

        var deliveriesToday = orders.Count(o => o.StartDate == today && o.Status == OrderStatus.Təsdiqlənmiş);
        var returnsToday = orders.Count(o => o.EndDate == today && o.Status == OrderStatus.TəhvilVerilmiş);
        var active = orders.Count(o => o.Status == OrderStatus.TəhvilVerilmiş);
        var pending = orders.Count(o => o.Status is OrderStatus.Qaralama or OrderStatus.Təsdiqlənmiş);
        var thisMonth = orders.Where(o => DateOnly.FromDateTime(o.CreatedAt.UtcDateTime) >= monthStart).ToList();

        return new ERP.Shared.Contracts.Mobile.EmployeeDashboardDto(
            EmployeeName: employeeName,
            DeliveriesToday: deliveriesToday,
            ReturnsToday: returnsToday,
            ActiveOrders: active,
            PendingOrders: pending,
            OrdersThisMonth: thisMonth.Count,
            RevenueThisMonth: thisMonth.Sum(o => o.Total),
            Currency: DefaultCurrency);
    }

    public async Task<ERP.Shared.Contracts.Mobile.EmployeeFinanceDto> GetEmployeeFinanceAsync(
        string employeeName, CancellationToken ct = default)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var weekStart = today.AddDays(-(((int)today.DayOfWeek + 6) % 7)); // həftənin bazar ertəsi
        var monthStart = new DateOnly(today.Year, today.Month, 1);
        var yearStart = new DateOnly(today.Year, 1, 1);

        var orders = await context.Orders
            .Where(o => o.CreatedByName == employeeName && o.Status != OrderStatus.Ləğv)
            .Select(o => new
            {
                o.CreatedAt,
                o.OrderType,
                Total = o.Lines.Sum(l => l.UnitPrice.Amount * l.Quantity)
            })
            .ToListAsync(ct);

        decimal SumFrom(DateOnly from) =>
            orders.Where(o => DateOnly.FromDateTime(o.CreatedAt.UtcDateTime) >= from).Sum(o => o.Total);

        var monthOrders = orders.Where(o => DateOnly.FromDateTime(o.CreatedAt.UtcDateTime) >= monthStart).ToList();

        return new ERP.Shared.Contracts.Mobile.EmployeeFinanceDto(
            EmployeeName: employeeName,
            RevenueToday: SumFrom(today),
            RevenueThisWeek: SumFrom(weekStart),
            RevenueThisMonth: SumFrom(monthStart),
            RevenueThisYear: SumFrom(yearStart),
            OrdersThisMonth: monthOrders.Count,
            RentalRevenueThisMonth: monthOrders.Where(o => o.OrderType == OrderType.İcarə).Sum(o => o.Total),
            SaleRevenueThisMonth: monthOrders.Where(o => o.OrderType == OrderType.Satış).Sum(o => o.Total),
            Currency: DefaultCurrency);
    }
}
