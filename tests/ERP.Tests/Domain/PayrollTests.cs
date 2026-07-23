using ERP.Domain.Exceptions;
using ERP.Domain.Modules.Hr;
using ERP.Domain.ValueObjects;
using Xunit;

namespace ERP.Tests.Domain;

public class PayrollTests
{
    private static readonly Guid Emp = Guid.NewGuid();

    private static Payroll Make(decimal bas, decimal bonus, decimal deduction) =>
        Payroll.Create("PAY-202607-ABC123", Emp, "Elçin", 2026, 7,
            Money.Create(bas), Money.Create(bonus), Money.Create(deduction));

    [Fact]
    public void NetSalary_is_base_plus_bonus_minus_deduction()
    {
        var p = Make(1000m, 200m, 150m);
        Assert.Equal(1050m, p.NetSalary.Amount);
    }

    [Fact]
    public void Deduction_over_base_plus_bonus_throws()
    {
        Assert.Throws<DomainException>(() => Make(1000m, 100m, 1200m));
    }

    [Fact]
    public void Invalid_month_throws()
    {
        Assert.Throws<DomainException>(() =>
            Payroll.Create("PAY-1", Emp, "Elçin", 2026, 13,
                Money.Create(1000m), Money.Zero(), Money.Zero()));
    }

    [Fact]
    public void MarkPaid_sets_status_and_date()
    {
        var p = Make(1000m, 0m, 0m);
        p.MarkPaid(new DateOnly(2026, 7, 31));
        Assert.Equal(PayrollStatus.Ödənilmiş, p.Status);
        Assert.Equal(new DateOnly(2026, 7, 31), p.PaidDate);
    }

    [Fact]
    public void Cannot_pay_twice()
    {
        var p = Make(1000m, 0m, 0m);
        p.MarkPaid(new DateOnly(2026, 7, 31));
        Assert.Throws<DomainException>(() => p.MarkPaid(new DateOnly(2026, 8, 1)));
    }
}
