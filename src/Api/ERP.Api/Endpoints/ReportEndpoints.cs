using ERP.Application.Common.Messaging;
using ERP.Application.Modules.Reports.Queries;

namespace ERP.Api.Endpoints;

/// <summary>Hesabat endpoint-ləri — idarə paneli, borclar, top məhsullar (TDD §11).</summary>
public static class ReportEndpoints
{
    public static IEndpointRouteBuilder MapReportEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/reports").WithTags("Reports");

        group.MapGet("/dashboard", async (ISender sender) =>
            Results.Ok(await sender.Send(new GetDashboardQuery())));

        group.MapGet("/outstanding", async (ISender sender) =>
            Results.Ok(await sender.Send(new GetOutstandingInvoicesQuery())));

        group.MapGet("/top-products", async (ISender sender, int top = 10) =>
            Results.Ok(await sender.Send(new GetTopProductsQuery(top))));

        return app;
    }
}
