using ERP.Application.Common.Interfaces;
using ERP.Application.Modules.Reports.Queries;
using ERP.Shared.Contracts.Reports;
using NSubstitute;
using Xunit;

namespace ERP.Tests.Application;

public class ReportQueryHandlersTests
{
    private readonly IReportService _reports = Substitute.For<IReportService>();

    [Fact]
    public async Task ProfitLoss_delegates_with_period()
    {
        var from = new DateOnly(2026, 1, 1);
        var to = new DateOnly(2026, 12, 31);
        var dto = new ProfitLossDto(1000m, 400m, 600m, "AZN", [], []);
        _reports.GetProfitLossAsync(from, to, Arg.Any<CancellationToken>()).Returns(dto);

        var handler = new GetProfitLossHandler(_reports);
        var result = await handler.Handle(new GetProfitLossQuery(from, to), default);

        Assert.Equal(600m, result.NetProfit);
        await _reports.Received(1).GetProfitLossAsync(from, to, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task MonthlyRevenue_delegates_with_year()
    {
        var dto = new MonthlyRevenueDto(2026, "AZN", []);
        _reports.GetMonthlyRevenueAsync(2026, Arg.Any<CancellationToken>()).Returns(dto);

        var handler = new GetMonthlyRevenueHandler(_reports);
        var result = await handler.Handle(new GetMonthlyRevenueQuery(2026), default);

        Assert.Equal(2026, result.Year);
        await _reports.Received(1).GetMonthlyRevenueAsync(2026, Arg.Any<CancellationToken>());
    }
}
