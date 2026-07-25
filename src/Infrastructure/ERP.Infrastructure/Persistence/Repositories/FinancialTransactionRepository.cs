using ERP.Application.Common.Interfaces;
using ERP.Application.Common.Models;
using ERP.Domain.Modules.Finance;
using Microsoft.EntityFrameworkCore;

namespace ERP.Infrastructure.Persistence.Repositories;

/// <summary>Maliyyə əməliyyatına xas repository implementasiyası (TDD §14).</summary>
public sealed class FinancialTransactionRepository(AppDbContext context)
    : Repository<FinancialTransaction>(context), IFinancialTransactionRepository
{
    public async Task<PagedResult<FinancialTransaction>> SearchAsync(
        string? search, TransactionType? type, int page, int pageSize, CancellationToken ct = default)
    {
        var query = Set.AsNoTracking().AsQueryable();

        if (type is not null)
            query = query.Where(t => t.Type == type);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var all = await query.ToListAsync(ct);
            return RankedSearch.Page(all, search, page, pageSize,
                primary: t => t.Category,
                secondary: t => [t.TransactionNumber, t.Description]);
        }

        var total = await query.CountAsync(ct);

        // TransactionNumber tarixlə başlayır (TRX-yyyyMMdd-...) → provider-safe sıralama, ən yeni öndə.
        var items = await query
            .OrderByDescending(t => t.TransactionNumber)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return new PagedResult<FinancialTransaction>
        {
            Items = items,
            TotalCount = total,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<(decimal income, decimal expense, int count)> GetSummaryAsync(CancellationToken ct = default)
    {
        // SQLite decimal-ı TEXT kimi saxlayır → server-side SUM etibarsızdır. Məbləğləri
        // klient tərəfə gətirib cəmləyirik (TDD §33 / ReportService ilə eyni yanaşma).
        var rows = await Set.AsNoTracking()
            .Select(t => new { t.Type, Amount = t.Amount.Amount })
            .ToListAsync(ct);

        var income = rows.Where(r => r.Type == TransactionType.Mədaxil).Sum(r => r.Amount);
        var expense = rows.Where(r => r.Type == TransactionType.Məxaric).Sum(r => r.Amount);
        return (income, expense, rows.Count);
    }
}
