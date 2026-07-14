using ERP.Domain.Exceptions;
using ERP.Domain.Modules.Invoices;
using ERP.Domain.Modules.Users;
using ERP.Domain.ValueObjects;
using Xunit;

namespace ERP.Tests.Domain;

public class InvoiceTests
{
    private static readonly DateOnly Today = new(2026, 7, 14);

    private static Invoice NewInvoice(decimal total = 750m) =>
        Invoice.Create("INV-1", Guid.NewGuid(), "ORD-1", Guid.NewGuid(), "Toy Sarayı", Today, Money.Create(total));

    [Fact]
    public void New_invoice_is_unpaid()
    {
        var inv = NewInvoice();
        Assert.Equal(InvoiceStatus.Ödənilməmiş, inv.Status);
        Assert.Equal(750m, inv.Balance.Amount);
    }

    [Fact]
    public void Partial_payment_is_partially_paid()
    {
        var inv = NewInvoice();
        inv.AddPayment(Money.Create(300m), Today, PaymentMethod.Nağd);
        Assert.Equal(InvoiceStatus.QismənÖdənilmiş, inv.Status);
        Assert.Equal(450m, inv.Balance.Amount);
    }

    [Fact]
    public void Full_payment_is_paid()
    {
        var inv = NewInvoice();
        inv.AddPayment(Money.Create(300m), Today, PaymentMethod.Nağd);
        inv.AddPayment(Money.Create(450m), Today, PaymentMethod.Köçürmə);
        Assert.Equal(InvoiceStatus.Ödənilmiş, inv.Status);
        Assert.Equal(0m, inv.Balance.Amount);
    }

    [Fact]
    public void Overpayment_throws()
    {
        var inv = NewInvoice();
        inv.AddPayment(Money.Create(300m), Today, PaymentMethod.Nağd);
        Assert.Throws<DomainException>(() => inv.AddPayment(Money.Create(500m), Today, PaymentMethod.Kart));
    }
}

public class UserPermissionTests
{
    [Fact]
    public void Admin_has_all_permissions()
    {
        Assert.Contains(Permissions.UsersManage, Permissions.ForRole(Role.Admin));
        Assert.Contains(Permissions.ReportsView, Permissions.ForRole(Role.Admin));
    }

    [Fact]
    public void Kassir_cannot_manage_users_or_view_reports()
    {
        var perms = Permissions.ForRole(Role.Kassir);
        Assert.DoesNotContain(Permissions.UsersManage, perms);
        Assert.DoesNotContain(Permissions.ReportsView, perms);
        Assert.Contains(Permissions.CustomersView, perms);
    }

    [Fact]
    public void Anbardar_can_edit_products_but_not_invoices()
    {
        var perms = Permissions.ForRole(Role.Anbardar);
        Assert.Contains(Permissions.ProductsEdit, perms);
        Assert.DoesNotContain(Permissions.InvoicesEdit, perms);
    }
}
