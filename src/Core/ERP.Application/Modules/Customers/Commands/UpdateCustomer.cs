using ERP.Application.Common.Interfaces;
using ERP.Application.Common.Models;
using ERP.Domain.Modules.Customers;
using ERP.Shared.Contracts.Customers;
using FluentValidation;
using MediatR;

namespace ERP.Application.Modules.Customers.Commands;

/// <summary>Mövcud müştərini yeniləyir (TDD §17 — Command).</summary>
public sealed record UpdateCustomerCommand(Guid Id, UpdateCustomerRequest Request)
    : IRequest<Result>;

public sealed class UpdateCustomerValidator : AbstractValidator<UpdateCustomerCommand>
{
    public UpdateCustomerValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Request.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Request.Phone).NotEmpty();
    }
}

public sealed class UpdateCustomerHandler(
    ICustomerRepository customers,
    IUnitOfWork unitOfWork)
    : IRequestHandler<UpdateCustomerCommand, Result>
{
    public async Task<Result> Handle(UpdateCustomerCommand request, CancellationToken ct)
    {
        var customer = await customers.GetByIdAsync(request.Id, ct);
        if (customer is null)
            return Result.Failure($"Müştəri tapılmadı: {request.Id}");

        var dto = request.Request;
        var phone = PhoneNumber.Create(dto.Phone);

        // Telefon dəyişibsə, başqa müştəri ilə toqquşmasın.
        if (phone.Value != customer.Phone.Value && await customers.PhoneExistsAsync(phone.Value, ct))
            return Result.Failure($"Bu telefon nömrəsi ilə başqa müştəri mövcuddur: {phone.Value}");

        var email = string.IsNullOrWhiteSpace(dto.Email) ? null : Email.Create(dto.Email);

        customer.Rename(dto.Name);
        customer.UpdateContact(phone, email);
        customer.ChangeAddress(CustomerMapping.ToAddress(dto.City, dto.AddressLine));
        customer.SetNotes(dto.Notes);
        if (dto.IsActive) customer.Activate(); else customer.Deactivate();

        customers.Update(customer);
        await unitOfWork.SaveChangesAsync(ct);

        return Result.Success();
    }
}
