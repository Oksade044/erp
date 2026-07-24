using ERP.Application.Common.Interfaces;
using ERP.Application.Modules.Reports.Queries;
using ERP.Shared.Contracts.Reports;
using NSubstitute;
using Xunit;

namespace ERP.Tests.Application;

public class GetDashboardHandlerTests
{
    private readonly IReportService _reports = Substitute.For<IReportService>();
    private readonly ICacheService _cache = Substitute.For<ICacheService>();

    private static DashboardDto Sample => new(
        CustomerCount: 3, ProductCount: 5, OrderCount: 2,
        DraftOrders: 1, ConfirmedOrders: 1, DeliveredOrders: 0, ReturnedOrders: 0, CancelledOrders: 0,
        TotalInvoiced: 100m, TotalPaid: 60m, TotalOutstanding: 40m, Currency: "AZN");

    [Fact]
    public async Task On_cache_miss_calls_report_service_via_factory()
    {
        // Keş boşdursa factory çağırılır (real ReportService işə düşür).
        _cache.GetOrCreateAsync(
                Arg.Any<string>(),
                Arg.Any<Func<CancellationToken, Task<DashboardDto>>>(),
                Arg.Any<TimeSpan?>(),
                Arg.Any<CancellationToken>())
            .Returns(ci => ci.Arg<Func<CancellationToken, Task<DashboardDto>>>()(CancellationToken.None));
        _reports.GetDashboardAsync(Arg.Any<CancellationToken>()).Returns(Sample);

        var handler = new GetDashboardHandler(_reports, _cache);
        var result = await handler.Handle(new GetDashboardQuery(), default);

        Assert.Equal(3, result.CustomerCount);
        await _reports.Received(1).GetDashboardAsync(Arg.Any<CancellationToken>());
        await _cache.Received(1).GetOrCreateAsync(
            CacheKeys.Dashboard,
            Arg.Any<Func<CancellationToken, Task<DashboardDto>>>(),
            Arg.Any<TimeSpan?>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task On_cache_hit_report_service_not_called()
    {
        // Keşdə varsa factory çağırılmır (ReportService işə düşmür).
        _cache.GetOrCreateAsync(
                Arg.Any<string>(),
                Arg.Any<Func<CancellationToken, Task<DashboardDto>>>(),
                Arg.Any<TimeSpan?>(),
                Arg.Any<CancellationToken>())
            .Returns(Sample);

        var handler = new GetDashboardHandler(_reports, _cache);
        var result = await handler.Handle(new GetDashboardQuery(), default);

        Assert.Equal(5, result.ProductCount);
        await _reports.DidNotReceive().GetDashboardAsync(Arg.Any<CancellationToken>());
    }
}
