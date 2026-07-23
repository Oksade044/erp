using ERP.Domain.Exceptions;
using ERP.Domain.Modules.Finance;
using ERP.Domain.Modules.Invoices;
using ERP.Domain.ValueObjects;
using Xunit;

namespace ERP.Tests.Domain;

public class FinancialTransactionTests
{
    private static FinancialTransaction Make(TransactionType type, decimal amount) =>
        FinancialTransaction.Create("TRX-20260723-ABC123", type, "İcarə gəliri",
            Money.Create(amount), new DateOnly(2026, 7, 23), PaymentMethod.Nağd);

    [Fact]
    public void Income_signed_amount_is_positive()
    {
        var t = Make(TransactionType.Mədaxil, 100m);
        Assert.Equal(100m, t.SignedAmount);
    }

    [Fact]
    public void Expense_signed_amount_is_negative()
    {
        var t = Make(TransactionType.Məxaric, 40m);
        Assert.Equal(-40m, t.SignedAmount);
    }

    [Fact]
    public void Zero_amount_throws()
    {
        Assert.Throws<DomainException>(() => Make(TransactionType.Mədaxil, 0m));
    }

    [Fact]
    public void Empty_category_throws()
    {
        Assert.Throws<DomainException>(() =>
            FinancialTransaction.Create("TRX-1", TransactionType.Mədaxil, "  ",
                Money.Create(10m), new DateOnly(2026, 7, 23), PaymentMethod.Nağd));
    }
}
