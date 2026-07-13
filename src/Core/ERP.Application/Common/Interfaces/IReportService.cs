using ERP.Shared.Contracts.Reports;

namespace ERP.Application.Common.Interfaces;

/// <summary>
/// Hesabat/analitika oxumaları (TDD §5 — ağır oxumalar üçün ayrı oxuma yolu).
/// İnterfeys Application-da, EF aqreqasiyaları Infrastructure-da.
/// </summary>
public interface IReportService
{
    Task<DashboardDto> GetDashboardAsync(CancellationToken ct = default);
    Task<IReadOnlyList<OutstandingInvoiceDto>> GetOutstandingInvoicesAsync(CancellationToken ct = default);
    Task<IReadOnlyList<TopProductDto>> GetTopProductsAsync(int top, CancellationToken ct = default);
}
