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

    [Fact]
    public void Installment_updates_paid_and_remaining_and_partial_status()
    {
        var p = Make(3000m, 0m, 0m);
        p.AddPayment(Money.Create(1500m), new DateOnly(2026, 7, 15), "Nağd", "1-ci hissə");

        Assert.Equal(1500m, p.PaidAmount.Amount);
        Assert.Equal(1500m, p.Remaining.Amount);
        Assert.Equal(PayrollStatus.QismənÖdənilmiş, p.Status);
    }

    [Fact]
    public void Installments_summing_to_net_complete_the_payroll()
    {
        var p = Make(3000m, 0m, 0m);
        p.AddPayment(Money.Create(1500m), new DateOnly(2026, 7, 15), "Nağd", null);
        p.AddPayment(Money.Create(1500m), new DateOnly(2026, 7, 31), "Nağd", null);

        Assert.Equal(0m, p.Remaining.Amount);
        Assert.Equal(PayrollStatus.Ödənilmiş, p.Status);
        Assert.Equal(new DateOnly(2026, 7, 31), p.PaidDate);
    }

    [Fact]
    public void Payment_over_remaining_throws()
    {
        var p = Make(3000m, 0m, 0m);
        p.AddPayment(Money.Create(1500m), new DateOnly(2026, 7, 15), "Nağd", null);
        Assert.Throws<DomainException>(() =>
            p.AddPayment(Money.Create(1600m), new DateOnly(2026, 7, 20), "Nağd", null));
    }

    [Fact]
    public void Bonus_increases_net_and_remaining_but_not_paid()
    {
        var p = Make(3000m, 0m, 0m);
        p.AddPayment(Money.Create(1500m), new DateOnly(2026, 7, 15), "Nağd", null);
        p.AddBonus(Money.Create(200m), new DateOnly(2026, 7, 16), "Nağd", "mükafat");

        Assert.Equal(3200m, p.NetSalary.Amount);
        Assert.Equal(1500m, p.PaidAmount.Amount);   // bonus ödənilmişə sayılmır
        Assert.Equal(1700m, p.Remaining.Amount);
        Assert.Equal(PayrollStatus.QismənÖdənilmiş, p.Status);
    }

    [Fact]
    public void MarkPaid_after_installment_pays_only_remaining()
    {
        var p = Make(3000m, 0m, 0m);
        p.AddPayment(Money.Create(1000m), new DateOnly(2026, 7, 15), "Nağd", null);
        var final = p.MarkPaid(new DateOnly(2026, 7, 31));

        Assert.Equal(2000m, final.Amount.Amount);   // yalnız qalıq borc
        Assert.Equal(0m, p.Remaining.Amount);
        Assert.Equal(PayrollStatus.Ödənilmiş, p.Status);
    }
}
