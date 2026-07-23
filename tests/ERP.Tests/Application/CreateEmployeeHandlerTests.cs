using ERP.Application.Common.Interfaces;
using ERP.Application.Modules.Hr.Commands;
using ERP.Domain.Modules.Hr;
using ERP.Shared.Contracts.Hr;
using NSubstitute;
using Xunit;

namespace ERP.Tests.Application;

public class CreateEmployeeHandlerTests
{
    private readonly IEmployeeRepository _employees = Substitute.For<IEmployeeRepository>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();

    private CreateEmployeeHandler Handler => new(_employees, _uow);

    private static CreateEmployeeCommand Command =>
        new(new CreateEmployeeRequest("Elçin Məmmədov", "Anbardar", "0551234567",
            new DateOnly(2026, 1, 15), 1200m, Department: "Anbar"));

    [Fact]
    public async Task Duplicate_phone_returns_failure()
    {
        _employees.PhoneExistsAsync("+994551234567").Returns(true);

        var result = await Handler.Handle(Command, default);

        Assert.True(result.IsFailure);
        await _uow.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task New_employee_is_saved()
    {
        _employees.PhoneExistsAsync("+994551234567").Returns(false);

        var result = await Handler.Handle(Command, default);

        Assert.True(result.IsSuccess);
        Assert.NotEqual(Guid.Empty, result.Value);
        await _employees.Received(1).AddAsync(Arg.Any<Employee>(), Arg.Any<CancellationToken>());
        await _uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
