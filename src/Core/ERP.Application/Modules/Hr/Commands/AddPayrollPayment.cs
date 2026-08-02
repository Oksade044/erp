using ERP.Application.Common.Interfaces;
using ERP.Application.Common.Messaging;
using ERP.Application.Common.Models;
using ERP.Domain.Modules.Finance;
using ERP.Domain.Modules.Hr;
using ERP.Domain.Modules.Invoices;
using ERP.Domain.ValueObjects;

namespace ERP.Application.Modules.Hr.Commands;

/// <summary>
/// Əməkhaqqıya hissə-hissə ödəniş əlavə edir (installment — məs. 3000 maaşın 1500-ü indi).
/// Ödəniş qalıq borcdan çox ola bilməz. Maliyyəyə yalnız ödənilən məbləğ qədər məxaric yazılır.
/// </summary>
public sealed record AddPayrollPaymentCommand(
    Guid Id, decimal Amount, DateOnly Date, string Method, string? Note) : IRequest<Result>;

public sealed class AddPayrollPaymentHandler(
    IPayrollRepository payrolls,
    IFinancialTransactionRepository transactions,
    IUnitOfWork unitOfWork)
    : IRequestHandler<AddPayrollPaymentCommand, Result>
{
    public async Task<Result> Handle(AddPayrollPaymentCommand request, CancellationToken ct)
    {
        var payroll = await payrolls.GetByIdAsync(request.Id, ct);
        if (payroll is null)
            return Result.Failure($"Əməkhaqqı tapılmadı: {request.Id}");

        var amount = Money.Create(request.Amount, payroll.BaseSalary.Currency);

        PayrollPayment payment;
        try
        {
            payment = payroll.AddPayment(amount, request.Date, request.Method, request.Note);
        }
        catch (ERP.Domain.Exceptions.DomainException ex)
        {
            return Result.Failure(ex.Message);
        }

        // Payroll izlənir → PaidAmount/Status UPDATE; ödəniş qeydi ayrıca INSERT.
        payrolls.AddPaymentRecord(payment);

        var number = $"TRX-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}";
        var expense = FinancialTransaction.Create(
            number,
            TransactionType.Məxaric,
            "Əməkhaqqı",
            amount,
            request.Date,
            ParseMethod(request.Method),
            $"{payroll.EmployeeName} — {payroll.Year}/{payroll.Month:D2} əməkhaqqı hissə ödəniş ({payroll.PayrollNumber})"
                + (string.IsNullOrWhiteSpace(request.Note) ? "" : $" — {request.Note}"));

        await transactions.AddAsync(expense, ct);
        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success();
    }

    internal static PaymentMethod ParseMethod(string? method) => method switch
    {
        "Nağd" => PaymentMethod.Nağd,
        "Kart" => PaymentMethod.Kart,
        _ => PaymentMethod.Köçürmə
    };
}

/// <summary>
/// Aya əlavə bonus verir — bonusu net maaşa əlavə edir və bonus ödənişi kimi qeyd olunur.
/// Maliyyəyə bonus məbləği qədər məxaric yazılır.
/// </summary>
public sealed record AddPayrollBonusCommand(
    Guid Id, decimal Amount, DateOnly Date, string Method, string? Note) : IRequest<Result>;

public sealed class AddPayrollBonusHandler(
    IPayrollRepository payrolls,
    IFinancialTransactionRepository transactions,
    IUnitOfWork unitOfWork)
    : IRequestHandler<AddPayrollBonusCommand, Result>
{
    public async Task<Result> Handle(AddPayrollBonusCommand request, CancellationToken ct)
    {
        var payroll = await payrolls.GetByIdAsync(request.Id, ct);
        if (payroll is null)
            return Result.Failure($"Əməkhaqqı tapılmadı: {request.Id}");

        var amount = Money.Create(request.Amount, payroll.BaseSalary.Currency);

        PayrollPayment payment;
        try
        {
            payment = payroll.AddBonus(amount, request.Date, request.Method, request.Note);
        }
        catch (ERP.Domain.Exceptions.DomainException ex)
        {
            return Result.Failure(ex.Message);
        }

        // Payroll izlənir → Bonus UPDATE; bonus qeydi ayrıca INSERT.
        payrolls.AddPaymentRecord(payment);

        var number = $"TRX-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}";
        var expense = FinancialTransaction.Create(
            number,
            TransactionType.Məxaric,
            "Bonus",
            amount,
            request.Date,
            AddPayrollPaymentHandler.ParseMethod(request.Method),
            $"{payroll.EmployeeName} — {payroll.Year}/{payroll.Month:D2} bonus ({payroll.PayrollNumber})"
                + (string.IsNullOrWhiteSpace(request.Note) ? "" : $" — {request.Note}"));

        await transactions.AddAsync(expense, ct);
        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success();
    }
}
