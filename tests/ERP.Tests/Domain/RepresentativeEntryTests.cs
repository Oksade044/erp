using ERP.Domain.Modules.Representatives;
using ERP.Domain.ValueObjects;
using Xunit;

namespace ERP.Tests.Domain;

public class RepresentativeEntryTests
{
    [Fact]
    public void Debt_reduces_balance_orders_increase_it()
    {
        var name = "Təmsilçi 1";
        var today = new DateOnly(2026, 8, 1);

        var debt = RepresentativeEntry.Create(name, today, RepEntryType.Borc, Money.Create(3000m));
        var order = RepresentativeEntry.Create(name, today, RepEntryType.Sifariş, Money.Create(2500m));
        var cancel = RepresentativeEntry.Create(name, today, RepEntryType.SifarişLəğvi, Money.Create(2500m));
        var payment = RepresentativeEntry.Create(name, today, RepEntryType.Ödəniş, Money.Create(500m));

        Assert.Equal(-3000m, debt.SignedAmount);   // borc mənfi
        Assert.Equal(2500m, order.SignedAmount);    // sifariş müsbət
        Assert.Equal(-2500m, cancel.SignedAmount);  // ləğv krediti geri alır
        Assert.Equal(500m, payment.SignedAmount);   // ödəniş müsbət

        // borc(-3000) + sifariş(2500) = -500 (spec #18)
        Assert.Equal(-500m, debt.SignedAmount + order.SignedAmount);
    }
}
