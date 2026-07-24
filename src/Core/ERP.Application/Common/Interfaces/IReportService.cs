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

    /// <summary>Mənfəət/Zərər — verilmiş dövr üzrə gəlir/xərc + kateqoriya bölgüsü (TDD §5).</summary>
    Task<ProfitLossDto> GetProfitLossAsync(DateOnly from, DateOnly to, CancellationToken ct = default);

    /// <summary>Aylıq gəlir/xərc analitikası — verilmiş il üçün 12 nöqtə (qrafik).</summary>
    Task<MonthlyRevenueDto> GetMonthlyRevenueAsync(int year, CancellationToken ct = default);
}
