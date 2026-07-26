using ERP.Application.Common.Messaging;
using ERP.Application.Modules.Reports.Queries;

namespace ERP.Api.Endpoints;

/// <summary>Hesabat endpoint-ləri — idarə paneli, borclar, top məhsullar (TDD §11).</summary>
public static class ReportEndpoints
{
    public static IEndpointRouteBuilder MapReportEndpoints(this IEndpointRouteBuilder app)
    {
        // Hesabatlar yalnız reports.view icazəsi olanlara (TDD §7).
        var group = app.MapGroup("/api/v1/reports").WithTags("Reports")
            .RequireAuthorization(ERP.Domain.Modules.Users.Permissions.ReportsView);

        group.MapGet("/dashboard", async (ISender sender) =>
            Results.Ok(await sender.Send(new GetDashboardQuery())));

        group.MapGet("/outstanding", async (ISender sender) =>
            Results.Ok(await sender.Send(new GetOutstandingInvoicesQuery())));

        group.MapGet("/top-products", async (ISender sender, int top = 10) =>
            Results.Ok(await sender.Send(new GetTopProductsQuery(top))));

        // Mənfəət/Zərər — dövr from/to (verilməzsə cari il).
        group.MapGet("/profit-loss", async (ISender sender, DateOnly? from, DateOnly? to) =>
        {
            var f = from ?? new DateOnly(DateTime.UtcNow.Year, 1, 1);
            var t = to ?? new DateOnly(DateTime.UtcNow.Year, 12, 31);
            return Results.Ok(await sender.Send(new GetProfitLossQuery(f, t)));
        });

        // Aylıq gəlir/xərc — verilmiş il (default cari).
        group.MapGet("/monthly-revenue", async (ISender sender, int? year) =>
            Results.Ok(await sender.Send(new GetMonthlyRevenueQuery(year ?? DateTime.UtcNow.Year))));

        // Müştəri hesabatı.
        group.MapGet("/customers", async (ISender sender) =>
            Results.Ok(await sender.Send(new GetCustomerReportQuery())));

        // Zədə/itki hesabatı.
        group.MapGet("/damages", async (ISender sender) =>
            Results.Ok(await sender.Send(new GetDamageReportQuery())));

        // İşçi performansı (#24) — dövr üzrə. Default: cari ay.
        group.MapGet("/employee-performance", async (ISender sender, DateOnly? from, DateOnly? to) =>
        {
            var f = from ?? new DateOnly(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
            var t = to ?? DateOnly.FromDateTime(DateTime.UtcNow);
            return Results.Ok(await sender.Send(new GetEmployeePerformanceQuery(f, t)));
        });

        return app;
    }
}
