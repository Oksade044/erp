using ERP.Application.Common.Interfaces;
using ERP.Application.Modules.Warehouses.Commands;
using ERP.Domain.Modules.Warehouses;
using ERP.Shared.Contracts.Warehouses;
using NSubstitute;
using Xunit;

namespace ERP.Tests.Application;

public class TransferStockHandlerTests
{
    private readonly IStockLevelRepository _levels = Substitute.For<IStockLevelRepository>();
    private readonly IWarehouseRepository _warehouses = Substitute.For<IWarehouseRepository>();
    private readonly IStockNotifier _notifier = Substitute.For<IStockNotifier>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();

    private TransferStockHandler Handler => new(_levels, _warehouses, _notifier, _uow);

    private static readonly Guid Prod = Guid.NewGuid();
    private static readonly Guid From = Guid.NewGuid();
    private static readonly Guid To = Guid.NewGuid();

    [Fact]
    public async Task Transfer_moves_quantity_between_warehouses()
    {
        var source = StockLevel.Create(Prod, "Stul", From, "Anbar-1", quantity: 20);
        var dest = StockLevel.Create(Prod, "Stul", To, "Anbar-2", quantity: 5);
        _levels.GetAsync(Prod, From).Returns(source);
        _levels.GetAsync(Prod, To).Returns(dest);

        var result = await Handler.Handle(
            new TransferStockCommand(new TransferStockRequest(Prod, From, To, 8)), default);

        Assert.True(result.IsSuccess);
        Assert.Equal(12, source.Quantity); // 20 - 8
        Assert.Equal(13, dest.Quantity);   // 5 + 8
        await _uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        // Hər iki anbarın dəyişikliyi canlı yayımlanmalıdır (TDD §38).
        await _notifier.Received(2).NotifyStockChangedAsync(
            Arg.Any<ERP.Shared.Contracts.Warehouses.StockChangedNotification>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Transfer_insufficient_source_fails()
    {
        var source = StockLevel.Create(Prod, "Stul", From, "Anbar-1", quantity: 3);
        _levels.GetAsync(Prod, From).Returns(source);

        var result = await Handler.Handle(
            new TransferStockCommand(new TransferStockRequest(Prod, From, To, 10)), default);

        Assert.True(result.IsFailure);
        await _uow.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Transfer_no_source_level_fails()
    {
        _levels.GetAsync(Prod, From).Returns((StockLevel?)null);

        var result = await Handler.Handle(
            new TransferStockCommand(new TransferStockRequest(Prod, From, To, 5)), default);

        Assert.True(result.IsFailure);
    }
}
