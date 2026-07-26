using ERP.Application.Common.Interfaces;
using ERP.Application.Common.Messaging;
using ERP.Application.Common.Models;
using ERP.Domain.Modules.Customers;
using ERP.Shared.Contracts.Suppliers;
using FluentValidation;

namespace ERP.Application.Modules.Suppliers.Commands;

/// <summary>Mövcud təchizatçını yeniləyir (TDD §17 — Command).</summary>
public sealed record UpdateSupplierCommand(Guid Id, UpdateSupplierRequest Request)
    : IRequest<Result>;

public sealed class UpdateSupplierValidator : AbstractValidator<UpdateSupplierCommand>
{
    public UpdateSupplierValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Request.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Request.Phone).NotEmpty();
    }
}

public sealed class UpdateSupplierHandler(
    ISupplierRepository suppliers,
    IUnitOfWork unitOfWork)
    : IRequestHandler<UpdateSupplierCommand, Result>
{
    public async Task<Result> Handle(UpdateSupplierCommand request, CancellationToken ct)
    {
        var supplier = await suppliers.GetByIdAsync(request.Id, ct);
        if (supplier is null)
            return Result.Failure($"Təchizatçı tapılmadı: {request.Id}");

        var dto = request.Request;
        var phone = PhoneNumber.CreateInternational(dto.Phone);

        if (phone.Value != supplier.Phone.Value && await suppliers.PhoneExistsAsync(phone.Value, ct))
            return Result.Failure($"Bu telefon nömrəsi ilə başqa təchizatçı mövcuddur: {phone.Value}");

        var email = string.IsNullOrWhiteSpace(dto.Email) ? null : Email.Create(dto.Email);

        supplier.Rename(dto.Name);
        supplier.UpdateContact(phone, email, dto.ContactPerson);
        supplier.ChangeAddress(SupplierMapping.ToAddress(dto.City, dto.AddressLine));
        supplier.SetTaxId(dto.TaxId);
        supplier.SetNotes(dto.Notes);
        supplier.SetExtras(dto.CompanyName, dto.Country, dto.WhatsApp, dto.WeChat, dto.Position);
        if (dto.IsActive) supplier.Activate(); else supplier.Deactivate();

        suppliers.Update(supplier);
        await unitOfWork.SaveChangesAsync(ct);

        return Result.Success();
    }
}
