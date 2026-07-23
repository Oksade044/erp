using ERP.Domain.Exceptions;
using ERP.Domain.Modules.Customers;
using ERP.Domain.Modules.Suppliers;
using Xunit;

namespace ERP.Tests.Domain;

public class SupplierTests
{
    private static PhoneNumber Phone => PhoneNumber.Create("0551234567");

    [Fact]
    public void Create_succeeds()
    {
        var s = Supplier.Create("Dekor MMC", Phone, contactPerson: "Elçin");
        Assert.Equal("Dekor MMC", s.Name);
        Assert.Equal("Elçin", s.ContactPerson);
        Assert.True(s.IsActive);
    }

    [Fact]
    public void Create_empty_name_throws()
    {
        Assert.Throws<DomainException>(() => Supplier.Create("  ", Phone));
    }

    [Fact]
    public void Blank_contact_person_becomes_null()
    {
        var s = Supplier.Create("Dekor MMC", Phone, contactPerson: "   ");
        Assert.Null(s.ContactPerson);
    }

    [Fact]
    public void Deactivate_sets_inactive()
    {
        var s = Supplier.Create("Dekor MMC", Phone);
        s.Deactivate();
        Assert.False(s.IsActive);
    }
}
