using ERP.Application.Common.Interfaces;
using ERP.Application.Common.Messaging;
using ERP.Application.Common.Models;
using ERP.Domain.Modules.Customers;
using ERP.Domain.Modules.Suppliers;
using ERP.Shared.Contracts.Suppliers;
using FluentValidation;

namespace ERP.Application.Modules.Suppliers.Commands;

/// <summary>Yeni təchizatçı yaradır (TDD §17 — Command). Uğurda Id qaytarır.</summary>
public sealed record CreateSupplierCommand(CreateSupplierRequest Request)
    : IRequest<Result<Guid>>;

public sealed class CreateSupplierValidator : AbstractValidator<CreateSupplierCommand>
{
    public CreateSupplierValidator()
    {
        RuleFor(x => x.Request.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Request.Phone).NotEmpty();
    }
}

public sealed class CreateSupplierHandler(
    ISupplierRepository suppliers,
    IUnitOfWork unitOfWork)
    : IRequestHandler<CreateSupplierCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CreateSupplierCommand request, CancellationToken ct)
    {
        var dto = request.Request;
        var phone = PhoneNumber.CreateInternational(dto.Phone);

        if (await suppliers.PhoneExistsAsync(phone.Value, ct))
            return Result.Failure<Guid>($"Bu telefon nömrəsi ilə təchizatçı artıq mövcuddur: {phone.Value}");

        var email = string.IsNullOrWhiteSpace(dto.Email) ? null : Email.Create(dto.Email);
        var address = SupplierMapping.ToAddress(dto.City, dto.AddressLine);

        var supplier = Supplier.Create(
            dto.Name, phone, dto.ContactPerson, email, address, dto.TaxId, dto.Notes,
            dto.CompanyName, dto.Country, dto.WhatsApp, dto.WeChat, dto.Position);

        await suppliers.AddAsync(supplier, ct);
        await unitOfWork.SaveChangesAsync(ct);

        return Result.Success(supplier.Id);
    }
}
