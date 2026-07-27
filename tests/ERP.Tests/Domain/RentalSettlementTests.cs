using ERP.Domain.Exceptions;
using ERP.Domain.Modules.Orders;
using ERP.Domain.ValueObjects;
using Xunit;

namespace ERP.Tests.Domain;

public class RentalSettlementTests
{
    private static RentalOrder DeliveredOrder()
    {
        var o = RentalOrder.Create("ORD-1", Guid.NewGuid(), "Aysel",
            new DateOnly(2026, 7, 1), new DateOnly(2026, 7, 3));
        o.AddLine(Guid.NewGuid(), "Çadır", 1, Money.Create(100m));
        o.Confirm();
        o.Deliver();
        return o;
    }

    [Fact]
    public void SetDeposit_stores_amount()
    {
        var o = DeliveredOrder();
        o.SetDeposit(Money.Create(200m));
        Assert.Equal(200m, o.Deposit!.Amount);
    }

    [Fact]
    public void Settle_computes_refund_after_charges()
    {
        var o = DeliveredOrder();
        o.SetDeposit(Money.Create(200m));
        o.Settle(Money.Create(50m), Money.Create(30m), "Bir çadır zədələnib");

        Assert.True(o.IsSettled);
        Assert.Equal(80m, o.TotalCharges.Amount);   // 50 + 30
        Assert.Equal(120m, o.DepositRefund.Amount);  // 200 - 80
    }

    [Fact]
    public void Refund_not_negative_when_charges_exceed_deposit()
    {
        var o = DeliveredOrder();
        o.SetDeposit(Money.Create(50m));
        o.Settle(Money.Create(80m), Money.Create(0m));
        Assert.Equal(0m, o.DepositRefund.Amount);
    }

    [Fact]
    public void Cannot_settle_draft_order()
    {
        var o = RentalOrder.Create("ORD-2", Guid.NewGuid(), "Aysel",
            new DateOnly(2026, 7, 1), new DateOnly(2026, 7, 3));
        Assert.Throws<DomainException>(() => o.Settle(Money.Create(10m), Money.Zero()));
    }

    [Fact]
    public void DepositStatus_reflects_lifecycle()
    {
        var noDeposit = DeliveredOrder();
        Assert.Equal(DepositStatus.Yoxdur, noDeposit.DepositStatus);

        var received = DeliveredOrder();
        received.SetDeposit(Money.Create(200m));
        Assert.Equal(DepositStatus.Alındı, received.DepositStatus);   // hələ hesablaşılmayıb

        var returned = DeliveredOrder();
        returned.SetDeposit(Money.Create(200m));
        returned.Settle(Money.Zero(), Money.Zero());
        Assert.Equal(DepositStatus.Qaytarıldı, returned.DepositStatus); // tutulma yox → tam qaytarıldı

        var partial = DeliveredOrder();
        partial.SetDeposit(Money.Create(200m));
        partial.Settle(Money.Create(50m), Money.Create(30m));
        Assert.Equal(DepositStatus.Qismən, partial.DepositStatus);     // 80 tutuldu, 120 qaldı

        var retained = DeliveredOrder();
        retained.SetDeposit(Money.Create(50m));
        retained.Settle(Money.Create(80m), Money.Zero());
        Assert.Equal(DepositStatus.Saxlanıldı, retained.DepositStatus); // tutulma depoziti udub
    }
}
