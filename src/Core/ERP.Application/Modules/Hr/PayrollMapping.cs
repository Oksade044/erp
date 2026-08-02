using ERP.Domain.Modules.Hr;
using ERP.Shared.Contracts.Hr;

namespace ERP.Application.Modules.Hr;

/// <summary>Payroll entity → DTO çevirmələri (TDD §12).</summary>
public static class PayrollMapping
{
    public static PayrollDto ToDto(this Payroll p) => new(
        Id: p.Id,
        PayrollNumber: p.PayrollNumber,
        EmployeeId: p.EmployeeId,
        EmployeeName: p.EmployeeName,
        Year: p.Year,
        Month: p.Month,
        BaseSalary: p.BaseSalary.Amount,
        Bonus: p.Bonus.Amount,
        Deduction: p.Deduction.Amount,
        NetSalary: p.NetSalary.Amount,
        PaidAmount: p.PaidAmount.Amount,
        Remaining: p.Remaining.Amount,
        Currency: p.BaseSalary.Currency,
        Status: p.Status.ToString(),
        PaidDate: p.PaidDate,
        Notes: p.Notes,
        CreatedAt: p.CreatedAt,
        Payments: p.Payments
            .OrderByDescending(x => x.Date)
            .Select(x => new PayrollPaymentDto(
                x.Id, x.Amount.Amount, x.Amount.Currency, x.Date, x.Method, x.Note, x.IsBonus))
            .ToList());
}
