using ERP.Domain.Exceptions;
using ERP.Domain.Modules.Purchases;
using ERP.Domain.ValueObjects;
using Xunit;

namespace ERP.Tests.Domain;

public class PurchaseOrderTests
{
    private static PurchaseOrder Draft()
    {
        var p = PurchaseOrder.Create("ALS-20260723-ABC123", Guid.NewGuid(), "Dekor MMC",
            new DateOnly(2026, 7, 23));
        p.AddLine(Guid.NewGuid(), "Stul", 10, Money.Create(5m));
        return p;
    }

    [Fact]
    public void Create_requires_supplier()
    {
        Assert.Throws<DomainException>(() =>
            PurchaseOrder.Create("ALS-1", Guid.Empty, "X", new DateOnly(2026, 7, 23)));
    }

    [Fact]
    public void Total_sums_lines()
    {
        var p = Draft();
        p.AddLine(Guid.NewGuid(), "Masa", 2, Money.Create(25m));
        Assert.Equal(100m, p.Total.Amount); // 10*5 + 2*25
    }

    [Fact]
    public void Duplicate_product_line_throws()
    {
        var p = PurchaseOrder.Create("ALS-1", Guid.NewGuid(), "X", new DateOnly(2026, 7, 23));
        var pid = Guid.NewGuid();
        p.AddLine(pid, "Stul", 5, Money.Create(3m));
        Assert.Throws<DomainException>(() => p.AddLine(pid, "Stul", 2, Money.Create(3m)));
    }

    [Fact]
    public void Lifecycle_draft_to_received()
    {
        var p = Draft();
        p.Confirm();
        Assert.Equal(PurchaseStatus.Təsdiqlənmiş, p.Status);
        p.Receive();
        Assert.Equal(PurchaseStatus.QəbulEdilmiş, p.Status);
    }

    [Fact]
    public void Cannot_receive_before_confirm()
    {
        var p = Draft();
        Assert.Throws<DomainException>(() => p.Receive());
    }

    [Fact]
    public void Cannot_cancel_received()
    {
        var p = Draft();
        p.Confirm();
        p.Receive();
        Assert.Throws<DomainException>(() => p.Cancel());
    }

    [Fact]
    public void Cannot_edit_lines_after_confirm()
    {
        var p = Draft();
        p.Confirm();
        Assert.Throws<DomainException>(() => p.AddLine(Guid.NewGuid(), "Masa", 1, Money.Create(1m)));
    }
}
