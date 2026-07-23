using ERP.Application.Common.Interfaces;
using ERP.Application.Common.Messaging;
using ERP.Application.Common.Models;
using ERP.Domain.Modules.Hr;
using ERP.Domain.ValueObjects;
using ERP.Shared.Contracts.Hr;
using FluentValidation;

namespace ERP.Application.Modules.Hr.Commands;

/// <summary>
/// Bir işçi üçün bir ay əməkhaqqı hesablaması yaradır. Baza maaş işçinin cari maaşından
/// snapshot kimi götürülür; bir işçi üçün bir dövrdə yalnız bir hesablama.
/// </summary>
public sealed record CreatePayrollCommand(CreatePayrollRequest Request) : IRequest<Result<Guid>>;

public sealed class CreatePayrollValidator : AbstractValidator<CreatePayrollCommand>
{
    public CreatePayrollValidator()
    {
        RuleFor(x => x.Request.EmployeeId).NotEmpty();
        RuleFor(x => x.Request.Year).GreaterThanOrEqualTo(2000);
        RuleFor(x => x.Request.Month).InclusiveBetween(1, 12);
        RuleFor(x => x.Request.Bonus).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Request.Deduction).GreaterThanOrEqualTo(0);
    }
}

public sealed class CreatePayrollHandler(
    IPayrollRepository payrolls,
    IEmployeeRepository employees,
    IUnitOfWork unitOfWork)
    : IRequestHandler<CreatePayrollCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CreatePayrollCommand request, CancellationToken ct)
    {
        var dto = request.Request;

        var employee = await employees.GetByIdAsync(dto.EmployeeId, ct);
        if (employee is null)
            return Result.Failure<Guid>($"İşçi tapılmadı: {dto.EmployeeId}");

        if (await payrolls.ExistsForPeriodAsync(dto.EmployeeId, dto.Year, dto.Month, ct))
            return Result.Failure<Guid>(
                $"Bu işçi üçün {dto.Year}/{dto.Month:D2} dövründə əməkhaqqı artıq hesablanıb.");

        var currency = employee.Salary.Currency;
        var number = $"PAY-{dto.Year}{dto.Month:D2}-{Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}";

        Payroll payroll;
        try
        {
            payroll = Payroll.Create(
                number, employee.Id, employee.FullName, dto.Year, dto.Month,
                employee.Salary,
                Money.Create(dto.Bonus, currency),
                Money.Create(dto.Deduction, currency));
        }
        catch (ERP.Domain.Exceptions.DomainException ex)
        {
            return Result.Failure<Guid>(ex.Message);
        }

        payroll.SetNotes(dto.Notes);

        await payrolls.AddAsync(payroll, ct);
        await unitOfWork.SaveChangesAsync(ct);

        return Result.Success(payroll.Id);
    }
}
