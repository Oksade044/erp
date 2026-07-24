using ERP.Application.Common.Interfaces;
using ERP.Application.Common.Messaging;
using ERP.Application.Common.Models;
using ERP.Domain.Modules.Customers;
using ERP.Shared.Contracts.Warehouses;
using FluentValidation;

namespace ERP.Application.Modules.Warehouses.Commands;

/// <summary>Mövcud anbarı yeniləyir (TDD §17 — Command).</summary>
public sealed record UpdateWarehouseCommand(Guid Id, UpdateWarehouseRequest Request) : IRequest<Result>;

public sealed class UpdateWarehouseValidator : AbstractValidator<UpdateWarehouseCommand>
{
    public UpdateWarehouseValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Request.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Request.Code).NotEmpty().MaximumLength(30);
    }
}

public sealed class UpdateWarehouseHandler(
    IWarehouseRepository warehouses,
    IUnitOfWork unitOfWork)
    : IRequestHandler<UpdateWarehouseCommand, Result>
{
    public async Task<Result> Handle(UpdateWarehouseCommand request, CancellationToken ct)
    {
        var warehouse = await warehouses.GetByIdAsync(request.Id, ct);
        if (warehouse is null)
            return Result.Failure($"Anbar tapılmadı: {request.Id}");

        var dto = request.Request;
        var code = dto.Code.Trim().ToUpperInvariant();

        if (code != warehouse.Code && await warehouses.CodeExistsAsync(code, ct))
            return Result.Failure($"Bu kod ilə başqa anbar mövcuddur: {code}");

        var phone = string.IsNullOrWhiteSpace(dto.Phone) ? null : PhoneNumber.Create(dto.Phone);

        warehouse.Rename(dto.Name);
        warehouse.ChangeCode(dto.Code);
        warehouse.ChangeAddress(WarehouseMapping.ToAddress(dto.City, dto.AddressLine));
        warehouse.SetPhone(phone);
        warehouse.SetNotes(dto.Notes);
        if (dto.IsActive) warehouse.Activate(); else warehouse.Deactivate();

        warehouses.Update(warehouse);
        await unitOfWork.SaveChangesAsync(ct);

        return Result.Success();
    }
}
