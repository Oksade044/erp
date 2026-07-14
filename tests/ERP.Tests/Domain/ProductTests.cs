using ERP.Domain.Exceptions;
using ERP.Domain.Modules.Products;
using ERP.Domain.ValueObjects;
using Xunit;

namespace ERP.Tests.Domain;

public class SkuTests
{
    [Fact]
    public void Create_normalizes_to_upper()
    {
        Assert.Equal("STUL-01", Sku.Create("stul-01").Value);
    }

    [Theory]
    [InlineData("")]
    [InlineData("a b c")]
    [InlineData("x")]
    public void Create_invalid_throws(string input)
    {
        Assert.Throws<DomainException>(() => Sku.Create(input));
    }
}

public class ProductTests
{
    private static Product NewProduct(int stock = 10) =>
        Product.Create(Sku.Create("STUL-01"), "Tiffany Stul", Money.Create(3.5m),
            ProductTrackingMode.Toplu, stock);

    [Fact]
    public void Create_succeeds_with_valid_data()
    {
        var p = NewProduct();
        Assert.Equal("Tiffany Stul", p.Name);
        Assert.Equal(10, p.StockQuantity);
        Assert.Equal(ProductTrackingMode.Toplu, p.TrackingMode);
    }

    [Fact]
    public void Create_negative_stock_throws()
    {
        Assert.Throws<DomainException>(
            () => Product.Create(Sku.Create("X-01"), "X", Money.Create(1m), ProductTrackingMode.Toplu, -1));
    }

    [Fact]
    public void AdjustStock_below_zero_throws()
    {
        var p = NewProduct(5);
        Assert.Throws<DomainException>(() => p.AdjustStock(-6));
    }

    [Fact]
    public void AdjustStock_updates_quantity()
    {
        var p = NewProduct(5);
        p.AdjustStock(3);
        Assert.Equal(8, p.StockQuantity);
    }

    [Fact]
    public void ChangePrice_updates_rental_price()
    {
        var p = NewProduct();
        p.ChangePrice(Money.Create(7m));
        Assert.Equal(7m, p.RentalPrice.Amount);
    }
}
