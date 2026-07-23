using ERP.Domain.Exceptions;
using ERP.Domain.Modules.Hr;
using Xunit;

namespace ERP.Tests.Domain;

public class AttendanceTests
{
    private static readonly Guid Emp = Guid.NewGuid();

    [Fact]
    public void WorkedHours_computed_from_times()
    {
        var a = Attendance.Create(Emp, "Elçin", new DateOnly(2026, 7, 23), AttendanceStatus.Gəlib,
            new TimeOnly(9, 0), new TimeOnly(18, 30));
        Assert.Equal(9.5m, a.WorkedHours);
    }

    [Fact]
    public void WorkedHours_zero_without_times()
    {
        var a = Attendance.Create(Emp, "Elçin", new DateOnly(2026, 7, 23), AttendanceStatus.Məzuniyyət);
        Assert.Equal(0m, a.WorkedHours);
    }

    [Fact]
    public void Checkout_before_checkin_throws()
    {
        Assert.Throws<DomainException>(() =>
            Attendance.Create(Emp, "Elçin", new DateOnly(2026, 7, 23), AttendanceStatus.Gəlib,
                new TimeOnly(18, 0), new TimeOnly(9, 0)));
    }

    [Fact]
    public void Empty_employee_throws()
    {
        Assert.Throws<DomainException>(() =>
            Attendance.Create(Guid.Empty, "Elçin", new DateOnly(2026, 7, 23), AttendanceStatus.Gəlib));
    }
}
