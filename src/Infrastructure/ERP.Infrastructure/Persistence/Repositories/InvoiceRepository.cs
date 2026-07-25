using ERP.Application.Common.Interfaces;
using ERP.Application.Common.Models;
using ERP.Domain.Modules.Invoices;
using Microsoft.EntityFrameworkCore;

namespace ERP.Infrastructure.Persistence.Repositories;

/// <summary>Fakturaya xas repository implementasiyası (TDD §14).</summary>
public sealed class InvoiceRepository(AppDbContext context)
    : Repository<Invoice>(context), IInvoiceRepository
{
    public async Task<Invoice?> GetByIdWithPaymentsAsync(Guid id, CancellationToken ct = default) =>
        await Set.Include(i => i.Payments).FirstOrDefaultAsync(i => i.Id == id, ct);

    public async Task<bool> ExistsForOrderAsync(Guid orderId, CancellationToken ct = default) =>
        await Set.AnyAsync(i => i.OrderId == orderId, ct);

    public void AttachNewPayment(Payment payment) =>
        Context.Set<Payment>().Add(payment);

    public async Task<PagedResult<Invoice>> SearchAsync(
        string? search, int page, int pageSize, CancellationToken ct = default)
    {
        var query = Set.AsNoTracking().Include(i => i.Payments);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var all = await query.ToListAsync(ct);
            return RankedSearch.Page(all, search, page, pageSize,
                primary: i => i.CustomerName,
                secondary: i => [i.InvoiceNumber, i.OrderNumber]);
        }

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(i => i.InvoiceNumber)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return new PagedResult<Invoice>
        {
            Items = items,
            TotalCount = total,
            Page = page,
            PageSize = pageSize
        };
    }
}
