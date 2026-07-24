using ERP.Application.Common.Interfaces;
using ERP.Application.Common.Messaging;
using ERP.Shared.Contracts.Reports;

namespace ERP.Application.Modules.Reports.Queries;

/// <summary>
/// İdarə paneli xülasəsi (TDD §17 — Query). Ağır aqreqasiya olduğu üçün keşlənir (TDD §37);
/// TTL keçdikdə yenilənir. Keçid Memory↔Redis konfiqurasiya ilə.
/// </summary>
public sealed record GetDashboardQuery : IRequest<DashboardDto>;

public sealed class GetDashboardHandler(IReportService reports, ICacheService cache)
    : IRequestHandler<GetDashboardQuery, DashboardDto>
{
    public Task<DashboardDto> Handle(GetDashboardQuery request, CancellationToken ct) =>
        cache.GetOrCreateAsync(CacheKeys.Dashboard, reports.GetDashboardAsync, ttl: null, ct);
}

/// <summary>Qalıq borcu olan fakturalar.</summary>
public sealed record GetOutstandingInvoicesQuery : IRequest<IReadOnlyList<OutstandingInvoiceDto>>;

public sealed class GetOutstandingInvoicesHandler(IReportService reports)
    : IRequestHandler<GetOutstandingInvoicesQuery, IReadOnlyList<OutstandingInvoiceDto>>
{
    public Task<IReadOnlyList<OutstandingInvoiceDto>> Handle(GetOutstandingInvoicesQuery request, CancellationToken ct) =>
        reports.GetOutstandingInvoicesAsync(ct);
}

/// <summary>Ən çox icarəyə verilən məhsullar.</summary>
public sealed record GetTopProductsQuery(int Top = 10) : IRequest<IReadOnlyList<TopProductDto>>;

public sealed class GetTopProductsHandler(IReportService reports)
    : IRequestHandler<GetTopProductsQuery, IReadOnlyList<TopProductDto>>
{
    public Task<IReadOnlyList<TopProductDto>> Handle(GetTopProductsQuery request, CancellationToken ct) =>
        reports.GetTopProductsAsync(request.Top, ct);
}

/// <summary>Mənfəət/Zərər hesabatı — dövr üzrə (TDD §5).</summary>
public sealed record GetProfitLossQuery(DateOnly From, DateOnly To) : IRequest<ProfitLossDto>;

public sealed class GetProfitLossHandler(IReportService reports)
    : IRequestHandler<GetProfitLossQuery, ProfitLossDto>
{
    public Task<ProfitLossDto> Handle(GetProfitLossQuery request, CancellationToken ct) =>
        reports.GetProfitLossAsync(request.From, request.To, ct);
}

/// <summary>Aylıq gəlir/xərc analitikası — verilmiş il (qrafik).</summary>
public sealed record GetMonthlyRevenueQuery(int Year) : IRequest<MonthlyRevenueDto>;

public sealed class GetMonthlyRevenueHandler(IReportService reports)
    : IRequestHandler<GetMonthlyRevenueQuery, MonthlyRevenueDto>
{
    public Task<MonthlyRevenueDto> Handle(GetMonthlyRevenueQuery request, CancellationToken ct) =>
        reports.GetMonthlyRevenueAsync(request.Year, ct);
}

/// <summary>Müştəri hesabatı — müştəri üzrə maliyyə xülasəsi.</summary>
public sealed record GetCustomerReportQuery : IRequest<IReadOnlyList<CustomerReportRowDto>>;

public sealed class GetCustomerReportHandler(IReportService reports)
    : IRequestHandler<GetCustomerReportQuery, IReadOnlyList<CustomerReportRowDto>>
{
    public Task<IReadOnlyList<CustomerReportRowDto>> Handle(GetCustomerReportQuery request, CancellationToken ct) =>
        reports.GetCustomerReportAsync(ct);
}

/// <summary>Zədə/itki hesabatı — tutulması olan sifarişlər.</summary>
public sealed record GetDamageReportQuery : IRequest<IReadOnlyList<DamageReportRowDto>>;

public sealed class GetDamageReportHandler(IReportService reports)
    : IRequestHandler<GetDamageReportQuery, IReadOnlyList<DamageReportRowDto>>
{
    public Task<IReadOnlyList<DamageReportRowDto>> Handle(GetDamageReportQuery request, CancellationToken ct) =>
        reports.GetDamageReportAsync(ct);
}
