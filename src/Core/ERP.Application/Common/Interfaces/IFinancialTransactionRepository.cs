using ERP.Application.Common.Models;
using ERP.Domain.Modules.Finance;

namespace ERP.Application.Common.Interfaces;

/// <summary>Maliyyə əməliyyatına xas repository (TDD §14).</summary>
public interface IFinancialTransactionRepository : IRepository<FinancialTransaction>
{
    /// <summary>Axtarış + növ filtri + səhifələmə (TDD §11).</summary>
    Task<PagedResult<FinancialTransaction>> SearchAsync(
        string? search, TransactionType? type, int page, int pageSize, CancellationToken ct = default);

    /// <summary>
    /// Kassa xülasəsi: ümumi mədaxil, məxaric, əməliyyat sayı.
    /// Pul cəmləri SQLite-da server-side etibarsızdır (decimal=TEXT) → klient tərəfdə cəmlənir (TDD §33 gotcha).
    /// </summary>
    Task<(decimal income, decimal expense, int count)> GetSummaryAsync(CancellationToken ct = default);
}
