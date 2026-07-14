using ERP.Domain.Exceptions;
using ERP.Domain.ValueObjects;
using Xunit;

namespace ERP.Tests.Domain;

public class MoneyTests
{
    [Fact]
    public void Create_rounds_to_two_decimals_and_defaults_to_AZN()
    {
        // decimal.Round bankir yuvarlaqlaşdırması (cütə) istifadə edir: 10.125 → 10.12.
        var money = Money.Create(10.125m);
        Assert.Equal(10.12m, money.Amount);
        Assert.Equal("AZN", money.Currency);
        // Qeyri-midpoint dəyər adi yuvarlaqlaşır.
        Assert.Equal(10.13m, Money.Create(10.129m).Amount);
    }

    [Fact]
    public void Create_negative_amount_throws()
    {
        Assert.Throws<System.ArgumentException>(() => Money.Create(-1m));
    }

    [Fact]
    public void Add_same_currency_sums()
    {
        var result = Money.Create(10m).Add(Money.Create(5m));
        Assert.Equal(15m, result.Amount);
    }

    [Fact]
    public void Add_different_currency_throws()
    {
        Assert.Throws<System.InvalidOperationException>(
            () => Money.Create(10m, "AZN").Add(Money.Create(5m, "USD")));
    }

    [Fact]
    public void Multiply_scales_amount()
    {
        Assert.Equal(30m, Money.Create(10m).Multiply(3).Amount);
    }

    [Fact]
    public void Equality_is_by_value()
    {
        Assert.Equal(Money.Create(10m, "AZN"), Money.Create(10m, "AZN"));
        Assert.NotEqual(Money.Create(10m, "AZN"), Money.Create(10m, "USD"));
    }
}
