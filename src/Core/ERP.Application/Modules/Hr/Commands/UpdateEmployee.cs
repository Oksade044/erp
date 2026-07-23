using ERP.Application.Common.Interfaces;
using ERP.Application.Common.Messaging;
using ERP.Application.Common.Models;
using ERP.Domain.Modules.Customers;
using ERP.Domain.ValueObjects;
using ERP.Shared.Contracts.Hr;
using FluentValidation;

namespace ERP.Application.Modules.Hr.Commands;

/// <summary>Mövcud işçini yeniləyir (TDD §17 — Command).</summary>
public sealed record UpdateEmployeeCommand(Guid Id, UpdateEmployeeRequest Request) : IRequest<Result>;

public sealed class UpdateEmployeeValidator : AbstractValidator<UpdateEmployeeCommand>
{
    public UpdateEmployeeValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Request.FullName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Request.Position).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Request.Phone).NotEmpty();
        RuleFor(x => x.Request.Salary).GreaterThanOrEqualTo(0);
    }
}

public sealed class UpdateEmployeeHandler(
    IEmployeeRepository employees,
    IUnitOfWork unitOfWork)
    : IRequestHandler<UpdateEmployeeCommand, Result>
{
    public async Task<Result> Handle(UpdateEmployeeCommand request, CancellationToken ct)
    {
        var employee = await employees.GetByIdAsync(request.Id, ct);
        if (employee is null)
            return Result.Failure($"İşçi tapılmadı: {request.Id}");

        var dto = request.Request;
        var phone = PhoneNumber.Create(dto.Phone);

        if (phone.Value != employee.Phone.Value && await employees.PhoneExistsAsync(phone.Value, ct))
            return Result.Failure($"Bu telefon nömrəsi ilə başqa işçi mövcuddur: {phone.Value}");

        var email = string.IsNullOrWhiteSpace(dto.Email) ? null : Email.Create(dto.Email);

        employee.UpdateDetails(dto.FullName, dto.Position, dto.Department, phone, email);
        employee.ChangeSalary(Money.Create(dto.Salary));
        employee.SetStatus(EmployeeMapping.ParseStatus(dto.Status));
        employee.SetNotes(dto.Notes);

        employees.Update(employee);
        await unitOfWork.SaveChangesAsync(ct);

        return Result.Success();
    }
}
