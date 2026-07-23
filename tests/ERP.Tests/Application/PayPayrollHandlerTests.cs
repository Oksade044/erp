using ERP.Application.Common.Interfaces;
using ERP.Application.Modules.Hr.Commands;
using ERP.Domain.Modules.Finance;
using ERP.Domain.Modules.Hr;
using ERP.Domain.ValueObjects;
using NSubstitute;
using Xunit;

namespace ERP.Tests.Application;

public class PayPayrollHandlerTests
{
    private readonly IPayrollRepository _payrolls = Substitute.For<IPayrollRepository>();
    private readonly IFinancialTransactionRepository _transactions = Substitute.For<IFinancialTransactionRepository>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();

    private PayPayrollHandler Handler => new(_payrolls, _transactions, _uow);

    [Fact]
    public async Task Pay_marks_paid_and_creates_expense_transaction()
    {
        var payroll = Payroll.Create("PAY-202607-ABC123", Guid.NewGuid(), "Elçin", 2026, 7,
            Money.Create(1000m), Money.Create(200m), Money.Create(100m)); // net 1100
        _payrolls.GetByIdAsync(payroll.Id).Returns(payroll);

        var result = await Handler.Handle(new PayPayrollCommand(payroll.Id), default);

        Assert.True(result.IsSuccess);
        Assert.Equal(PayrollStatus.Ödənilmiş, payroll.Status);
        // Maliyyəyə net maaş qədər məxaric yazılmalıdır.
        await _transactions.Received(1).AddAsync(
            Arg.Is<FinancialTransaction>(t =>
                t.Type == TransactionType.Məxaric && t.Amount.Amount == 1100m && t.Category == "Əməkhaqqı"),
            Arg.Any<CancellationToken>());
        await _uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Pay_missing_payroll_fails()
    {
        _payrolls.GetByIdAsync(Arg.Any<Guid>()).Returns((Payroll?)null);

        var result = await Handler.Handle(new PayPayrollCommand(Guid.NewGuid()), default);

        Assert.True(result.IsFailure);
        await _uow.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
