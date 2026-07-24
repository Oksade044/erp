using ERP.Domain.Exceptions;
using ERP.Domain.Modules.Warehouses;
using Xunit;

namespace ERP.Tests.Domain;

public class WarehouseTests
{
    [Fact]
    public void Create_normalizes_code_uppercase()
    {
        var w = Warehouse.Create("Mərkəzi anbar", "anbar-1");
        Assert.Equal("ANBAR-1", w.Code);
        Assert.True(w.IsActive);
    }

    [Fact]
    public void Create_empty_name_throws()
    {
        Assert.Throws<DomainException>(() => Warehouse.Create("  ", "A1"));
    }

    [Fact]
    public void Create_empty_code_throws()
    {
        Assert.Throws<DomainException>(() => Warehouse.Create("Anbar", "  "));
    }

    [Fact]
    public void Deactivate_sets_inactive()
    {
        var w = Warehouse.Create("Anbar", "A1");
        w.Deactivate();
        Assert.False(w.IsActive);
    }
}
