using ERP.Application.Common.Interfaces;
using ERP.Application.Common.Messaging;
using ERP.Shared.Contracts.Reports;

namespace ERP.Application.Modules.Reports.Queries;

/// <summary>İdarə paneli xülasəsi (TDD §17 — Query). Nazik handler → IReportService.</summary>
public sealed record GetDashboardQuery : IRequest<DashboardDto>;

public sealed class GetDashboardHandler(IReportService reports)
    : IRequestHandler<GetDashboardQuery, DashboardDto>
{
    public Task<DashboardDto> Handle(GetDashboardQuery request, CancellationToken ct) =>
        reports.GetDashboardAsync(ct);
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
