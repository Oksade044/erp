using ERP.Domain.Exceptions;
using ERP.Domain.Modules.Customers;
using Xunit;

namespace ERP.Tests.Domain;

public class PhoneNumberTests
{
    [Theory]
    [InlineData("0551234567", "+994551234567")]
    [InlineData("551234567", "+994551234567")]
    [InlineData("+994551234567", "+994551234567")]
    [InlineData("055 123 45 67", "+994551234567")]
    public void Create_normalizes_to_e164(string input, string expected)
    {
        Assert.Equal(expected, PhoneNumber.Create(input).Value);
    }

    [Theory]
    [InlineData("")]
    [InlineData("123")]
    [InlineData(null)]
    public void Create_invalid_throws(string? input)
    {
        Assert.Throws<DomainException>(() => PhoneNumber.Create(input));
    }
}

public class EmailTests
{
    [Fact]
    public void Create_lowercases_valid_email()
    {
        Assert.Equal("test@example.az", Email.Create("Test@Example.AZ").Value);
    }

    [Theory]
    [InlineData("notanemail")]
    [InlineData("a@b")]
    public void Create_invalid_throws(string input)
    {
        Assert.Throws<DomainException>(() => Email.Create(input));
    }
}

public class CustomerTests
{
    private static PhoneNumber Phone => PhoneNumber.Create("0551234567");

    [Fact]
    public void Create_individual_succeeds()
    {
        var c = Customer.Create(CustomerType.Fərdi, "Aysel", Phone);
        Assert.Equal("Aysel", c.Name);
        Assert.True(c.IsActive);
    }

    [Fact]
    public void Create_empty_name_throws()
    {
        Assert.Throws<DomainException>(() => Customer.Create(CustomerType.Fərdi, "  ", Phone));
    }

    [Fact]
    public void TaxId_only_allowed_for_corporate()
    {
        Assert.Throws<DomainException>(
            () => Customer.Create(CustomerType.Fərdi, "Aysel", Phone, taxId: "123"));

        var corp = Customer.Create(CustomerType.Korporativ, "MMC", Phone, taxId: "123");
        Assert.Equal("123", corp.TaxId);
    }

    [Fact]
    public void Deactivate_sets_inactive()
    {
        var c = Customer.Create(CustomerType.Fərdi, "Aysel", Phone);
        c.Deactivate();
        Assert.False(c.IsActive);
    }
}
