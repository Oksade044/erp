using ERP.Application.Common.Interfaces;
using ERP.Application.Common.Models;
using ERP.Domain.Modules.Customers;
using ERP.Shared.Contracts.Customers;
using FluentValidation;
using MediatR;

namespace ERP.Application.Modules.Customers.Commands;

/// <summary>Yeni müştəri yaradır (TDD §17 — Command). Uğurda müştəri Id-sini qaytarır.</summary>
public sealed record CreateCustomerCommand(CreateCustomerRequest Request)
    : IRequest<Result<Guid>>;

public sealed class CreateCustomerValidator : AbstractValidator<CreateCustomerCommand>
{
    public CreateCustomerValidator()
    {
        RuleFor(x => x.Request.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Request.Phone).NotEmpty();
        RuleFor(x => x.Request.Type).NotEmpty();
    }
}

public sealed class CreateCustomerHandler(
    ICustomerRepository customers,
    IUnitOfWork unitOfWork)
    : IRequestHandler<CreateCustomerCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CreateCustomerCommand request, CancellationToken ct)
    {
        var dto = request.Request;

        var type = CustomerMapping.ParseType(dto.Type);
        var phone = PhoneNumber.Create(dto.Phone);

        // İkiqat qeydin qarşısı — eyni telefon (TDD §33 unikal biznes qaydası).
        if (await customers.PhoneExistsAsync(phone.Value, ct))
            return Result.Failure<Guid>($"Bu telefon nömrəsi ilə müştəri artıq mövcuddur: {phone.Value}");

        var email = string.IsNullOrWhiteSpace(dto.Email) ? null : Email.Create(dto.Email);
        var address = CustomerMapping.ToAddress(dto.City, dto.AddressLine);

        var customer = Customer.Create(type, dto.Name, phone, email, address, dto.TaxId, dto.Notes);

        await customers.AddAsync(customer, ct);
        await unitOfWork.SaveChangesAsync(ct);

        return Result.Success(customer.Id);
    }
}
