using ERP.Application.Common.Interfaces;
using ERP.Application.Modules.Suppliers.Commands;
using ERP.Domain.Modules.Suppliers;
using ERP.Shared.Contracts.Suppliers;
using NSubstitute;
using Xunit;

namespace ERP.Tests.Application;

public class CreateSupplierHandlerTests
{
    private readonly ISupplierRepository _suppliers = Substitute.For<ISupplierRepository>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();

    private CreateSupplierHandler Handler => new(_suppliers, _uow);

    private static CreateSupplierCommand Command =>
        new(new CreateSupplierRequest("Dekor MMC", "0551234567"));

    [Fact]
    public async Task Duplicate_phone_returns_failure()
    {
        _suppliers.PhoneExistsAsync("+994551234567").Returns(true);

        var result = await Handler.Handle(Command, default);

        Assert.True(result.IsFailure);
        await _uow.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task New_supplier_is_saved()
    {
        _suppliers.PhoneExistsAsync("+994551234567").Returns(false);

        var result = await Handler.Handle(Command, default);

        Assert.True(result.IsSuccess);
        Assert.NotEqual(Guid.Empty, result.Value);
        await _suppliers.Received(1).AddAsync(Arg.Any<Supplier>(), Arg.Any<CancellationToken>());
        await _uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
