using ERP.Application.Common.Interfaces;
using ERP.Application.Modules.Hr.Commands;
using ERP.Domain.Modules.Customers;
using ERP.Domain.Modules.Hr;
using ERP.Domain.ValueObjects;
using ERP.Shared.Contracts.Hr;
using NSubstitute;
using Xunit;

namespace ERP.Tests.Application;

public class CreateAttendanceHandlerTests
{
    private readonly IAttendanceRepository _attendance = Substitute.For<IAttendanceRepository>();
    private readonly IEmployeeRepository _employees = Substitute.For<IEmployeeRepository>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();

    private CreateAttendanceHandler Handler => new(_attendance, _employees, _uow);

    private static Employee Employee =>
        Employee.Create("EMP-1", "Elçin", "Anbardar", PhoneNumber.Create("0551234567"),
            new DateOnly(2026, 1, 1), Money.Create(1000m));

    private static CreateAttendanceCommand Command(Guid empId) =>
        new(new CreateAttendanceRequest(empId, new DateOnly(2026, 7, 23), "Gəlib"));

    [Fact]
    public async Task Duplicate_for_day_returns_failure()
    {
        var emp = Employee;
        _employees.GetByIdAsync(emp.Id).Returns(emp);
        _attendance.ExistsForEmployeeDateAsync(emp.Id, new DateOnly(2026, 7, 23)).Returns(true);

        var result = await Handler.Handle(Command(emp.Id), default);

        Assert.True(result.IsFailure);
        await _uow.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Valid_record_is_saved()
    {
        var emp = Employee;
        _employees.GetByIdAsync(emp.Id).Returns(emp);
        _attendance.ExistsForEmployeeDateAsync(emp.Id, new DateOnly(2026, 7, 23)).Returns(false);

        var result = await Handler.Handle(Command(emp.Id), default);

        Assert.True(result.IsSuccess);
        await _attendance.Received(1).AddAsync(Arg.Any<Attendance>(), Arg.Any<CancellationToken>());
        await _uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
