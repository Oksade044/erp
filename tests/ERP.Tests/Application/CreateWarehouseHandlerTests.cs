using ERP.Application.Common.Interfaces;
using ERP.Application.Modules.Warehouses.Commands;
using ERP.Domain.Modules.Warehouses;
using ERP.Shared.Contracts.Warehouses;
using NSubstitute;
using Xunit;

namespace ERP.Tests.Application;

public class CreateWarehouseHandlerTests
{
    private readonly IWarehouseRepository _warehouses = Substitute.For<IWarehouseRepository>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();

    private CreateWarehouseHandler Handler => new(_warehouses, _uow);

    private static CreateWarehouseCommand Command =>
        new(new CreateWarehouseRequest("Mərkəzi anbar", "anbar-1"));

    [Fact]
    public async Task Duplicate_code_returns_failure()
    {
        _warehouses.CodeExistsAsync("ANBAR-1").Returns(true);

        var result = await Handler.Handle(Command, default);

        Assert.True(result.IsFailure);
        await _uow.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task New_warehouse_is_saved()
    {
        _warehouses.CodeExistsAsync("ANBAR-1").Returns(false);

        var result = await Handler.Handle(Command, default);

        Assert.True(result.IsSuccess);
        Assert.NotEqual(Guid.Empty, result.Value);
        await _warehouses.Received(1).AddAsync(Arg.Any<Warehouse>(), Arg.Any<CancellationToken>());
        await _uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
