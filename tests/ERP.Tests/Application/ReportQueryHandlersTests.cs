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

    [Fact]
    public async Task CustomerReport_delegates()
    {
        IReadOnlyList<CustomerReportRowDto> rows =
            [new(Guid.NewGuid(), "Aysel", 2, 300m, 200m, 100m, "AZN")];
        _reports.GetCustomerReportAsync(Arg.Any<CancellationToken>()).Returns(rows);

        var handler = new GetCustomerReportHandler(_reports);
        var result = await handler.Handle(new GetCustomerReportQuery(), default);

        Assert.Single(result);
        Assert.Equal(100m, result[0].Outstanding);
    }

    [Fact]
    public async Task DamageReport_delegates()
    {
        IReadOnlyList<DamageReportRowDto> rows =
            [new("ORD-1", "Aysel", 50m, 30m, 80m, "AZN", "zədə")];
        _reports.GetDamageReportAsync(Arg.Any<CancellationToken>()).Returns(rows);

        var handler = new GetDamageReportHandler(_reports);
        var result = await handler.Handle(new GetDamageReportQuery(), default);

        Assert.Single(result);
        Assert.Equal(80m, result[0].TotalCharges);
    }

    [Fact]
    public async Task RentalCalendar_delegates_with_period()
    {
        var from = new DateOnly(2026, 7, 1);
        var to = new DateOnly(2026, 7, 31);
        IReadOnlyList<RentalCalendarEntryDto> rows =
            [new(Guid.NewGuid(), "ORD-1", "Aysel",
                new DateOnly(2026, 7, 10), new DateOnly(2026, 7, 12),
                "Təsdiqlənmiş", 250m, "AZN", true, true)];
        _reports.GetRentalCalendarAsync(from, to, Arg.Any<CancellationToken>()).Returns(rows);

        var handler = new GetRentalCalendarHandler(_reports);
        var result = await handler.Handle(new GetRentalCalendarQuery(from, to), default);

        Assert.Single(result);
        Assert.True(result[0].DeliversInRange);
        await _reports.Received(1).GetRentalCalendarAsync(from, to, Arg.Any<CancellationToken>());
    }
}
