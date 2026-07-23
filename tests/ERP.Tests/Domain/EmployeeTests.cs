using ERP.Domain.Exceptions;
using ERP.Domain.Modules.Customers;
using ERP.Domain.Modules.Hr;
using ERP.Domain.ValueObjects;
using Xunit;

namespace ERP.Tests.Domain;

public class EmployeeTests
{
    private static PhoneNumber Phone => PhoneNumber.Create("0551234567");

    private static Employee Make() =>
        Employee.Create("EMP-ABC12345", "Elçin Məmmədov", "Anbardar", Phone,
            new DateOnly(2026, 1, 15), Money.Create(1200m), department: "Anbar");

    [Fact]
    public void Create_succeeds()
    {
        var e = Make();
        Assert.Equal("Elçin Məmmədov", e.FullName);
        Assert.Equal("Anbardar", e.Position);
        Assert.Equal(1200m, e.Salary.Amount);
        Assert.Equal(EmployeeStatus.İşləyir, e.Status);
    }

    [Fact]
    public void Create_empty_position_throws()
    {
        Assert.Throws<DomainException>(() =>
            Employee.Create("EMP-1", "Ad", "  ", Phone, new DateOnly(2026, 1, 1), Money.Create(500m)));
    }

    [Fact]
    public void ChangeSalary_updates()
    {
        var e = Make();
        e.ChangeSalary(Money.Create(1500m));
        Assert.Equal(1500m, e.Salary.Amount);
    }

    [Fact]
    public void SetStatus_terminated()
    {
        var e = Make();
        e.SetStatus(EmployeeStatus.İşdənÇıxmış);
        Assert.Equal(EmployeeStatus.İşdənÇıxmış, e.Status);
    }
}
