using ERP.Application.Common.Interfaces;
using ERP.Application.Common.Messaging;
using ERP.Application.Common.Models;
using ERP.Domain.Modules.Finance;
using ERP.Domain.Modules.Invoices;

namespace ERP.Application.Modules.Hr.Commands;

/// <summary>
/// Hesablanmış əməkhaqqını ödənilmiş kimi işarələyir VƏ Maliyyə modulunda net maaş qədər
/// məxaric əməliyyatı yaradır (kassadan çıxış). Hər ikisi tək transaction-da (TDD §15).
/// </summary>
public sealed record PayPayrollCommand(Guid Id) : IRequest<Result>;

public sealed class PayPayrollHandler(
    IPayrollRepository payrolls,
    IFinancialTransactionRepository transactions,
    IUnitOfWork unitOfWork)
    : IRequestHandler<PayPayrollCommand, Result>
{
    public async Task<Result> Handle(PayPayrollCommand request, CancellationToken ct)
    {
        var payroll = await payrolls.GetByIdAsync(request.Id, ct);
        if (payroll is null)
            return Result.Failure($"Əməkhaqqı tapılmadı: {request.Id}");

        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        // Yalnız qalıq borc qədər ödəniş edilir (əvvəl hissə-hissə ödənilibsə nəzərə alınır).
        var amountPaid = payroll.Remaining;
        if (amountPaid.Amount <= 0m)
            return Result.Failure("Bu əməkhaqqı üzrə qalıq borc yoxdur.");

        // Payroll izlənir → PaidAmount/Status dəyişikliyi UPDATE; ödəniş qeydi isə ayrıca INSERT.
        var payment = payroll.MarkPaid(today);
        payrolls.AddPaymentRecord(payment);

        // Maliyyəyə məxaric (kassadan çıxış) — yalnız indi ödənilən məbləğ qədər.
        var number = $"TRX-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}";
        var expense = FinancialTransaction.Create(
            number,
            TransactionType.Məxaric,
            "Əməkhaqqı",
            amountPaid,
            today,
            PaymentMethod.Köçürmə,
            $"{payroll.EmployeeName} — {payroll.Year}/{payroll.Month:D2} əməkhaqqı (tam ödəniş, {payroll.PayrollNumber})");

        await transactions.AddAsync(expense, ct);
        await unitOfWork.SaveChangesAsync(ct);

        return Result.Success();
    }
}
