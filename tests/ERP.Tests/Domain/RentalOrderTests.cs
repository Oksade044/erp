using ERP.Domain.Exceptions;
using ERP.Domain.Modules.Orders;
using ERP.Domain.ValueObjects;
using Xunit;

namespace ERP.Tests.Domain;

public class RentalOrderTests
{
    private static readonly DateOnly Start = new(2026, 8, 1);
    private static readonly DateOnly End = new(2026, 8, 3);

    private static RentalOrder NewOrder() =>
        RentalOrder.Create("ORD-1", Guid.NewGuid(), "Toy Sarayı", Start, End);

    [Fact]
    public void Create_end_before_start_throws()
    {
        Assert.Throws<DomainException>(
            () => RentalOrder.Create("ORD-1", Guid.NewGuid(), "X", End, Start));
    }

    [Fact]
    public void AddLine_computes_total()
    {
        var order = NewOrder();
        order.AddLine(Guid.NewGuid(), "LED", 5, Money.Create(150m));
        order.AddLine(Guid.NewGuid(), "Stul", 50, Money.Create(4m));
        Assert.Equal(950m, order.Total.Amount);
    }

    [Fact]
    public void AddLine_duplicate_product_throws()
    {
        var order = NewOrder();
        var pid = Guid.NewGuid();
        order.AddLine(pid, "LED", 5, Money.Create(150m));
        Assert.Throws<DomainException>(() => order.AddLine(pid, "LED", 3, Money.Create(150m)));
    }

    [Fact]
    public void Confirm_empty_order_throws()
    {
        Assert.Throws<DomainException>(() => NewOrder().Confirm());
    }

    [Fact]
    public void Full_lifecycle_transitions()
    {
        var order = NewOrder();
        order.AddLine(Guid.NewGuid(), "LED", 1, Money.Create(150m));

        order.Confirm();
        Assert.Equal(OrderStatus.Təsdiqlənmiş, order.Status);
        Assert.True(order.ReservesStock);

        order.Deliver();
        Assert.Equal(OrderStatus.TəhvilVerilmiş, order.Status);

        order.Return();
        Assert.Equal(OrderStatus.Qaytarılmış, order.Status);
        Assert.False(order.ReservesStock);
    }

    [Fact]
    public void Cannot_add_line_after_confirm()
    {
        var order = NewOrder();
        order.AddLine(Guid.NewGuid(), "LED", 1, Money.Create(150m));
        order.Confirm();
        Assert.Throws<DomainException>(() => order.AddLine(Guid.NewGuid(), "X", 1, Money.Create(1m)));
    }

    [Fact]
    public void Deliver_without_confirm_throws()
    {
        var order = NewOrder();
        order.AddLine(Guid.NewGuid(), "LED", 1, Money.Create(150m));
        Assert.Throws<DomainException>(() => order.Deliver());
    }
}
