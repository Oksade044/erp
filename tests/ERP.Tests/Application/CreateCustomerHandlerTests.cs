using ERP.Application.Common.Interfaces;
using ERP.Application.Modules.Customers.Commands;
using ERP.Domain.Modules.Customers;
using ERP.Shared.Contracts.Customers;
using NSubstitute;
using Xunit;

namespace ERP.Tests.Application;

public class CreateCustomerHandlerTests
{
    private readonly ICustomerRepository _customers = Substitute.For<ICustomerRepository>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();

    private CreateCustomerHandler Handler => new(_customers, _uow);

    private static CreateCustomerCommand Command =>
        new(new CreateCustomerRequest("Fərdi", "Aysel", "0551234567"));

    [Fact]
    public async Task Duplicate_phone_returns_failure()
    {
        _customers.PhoneExistsAsync("+994551234567").Returns(true);

        var result = await Handler.Handle(Command, default);

        Assert.True(result.IsFailure);
        await _uow.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task New_customer_is_saved()
    {
        _customers.PhoneExistsAsync("+994551234567").Returns(false);

        var result = await Handler.Handle(Command, default);

        Assert.True(result.IsSuccess);
        Assert.NotEqual(Guid.Empty, result.Value);
        await _customers.Received(1).AddAsync(Arg.Any<Customer>(), Arg.Any<CancellationToken>());
        await _uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
