using ERP.Domain.Exceptions;
using ERP.Domain.Modules.Warehouses;
using Xunit;

namespace ERP.Tests.Domain;

public class StockLevelTests
{
    private static readonly Guid Prod = Guid.NewGuid();
    private static readonly Guid Wh = Guid.NewGuid();

    private static StockLevel Make(int qty, int min = 0) =>
        StockLevel.Create(Prod, "Stul", Wh, "Anbar-1", qty, min);

    [Fact]
    public void Increase_adds_quantity()
    {
        var s = Make(10);
        s.Increase(5);
        Assert.Equal(15, s.Quantity);
    }

    [Fact]
    public void Decrease_reduces_quantity()
    {
        var s = Make(10);
        s.Decrease(4);
        Assert.Equal(6, s.Quantity);
    }

    [Fact]
    public void Decrease_more_than_available_throws()
    {
        var s = Make(3);
        Assert.Throws<DomainException>(() => s.Decrease(5));
    }

    [Fact]
    public void IsLow_when_below_min()
    {
        Assert.True(Make(2, min: 5).IsLow);
        Assert.False(Make(6, min: 5).IsLow);
    }
}
