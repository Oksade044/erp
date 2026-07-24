using ERP.Application.Common.Interfaces;
using ERP.Application.Common.Messaging;
using ERP.Application.Common.Models;
using ERP.Domain.Modules.Customers;
using ERP.Domain.Modules.Warehouses;
using ERP.Shared.Contracts.Warehouses;
using FluentValidation;

namespace ERP.Application.Modules.Warehouses.Commands;

/// <summary>Yeni anbar yaradır (TDD §17 — Command). Uğurda Id qaytarır.</summary>
public sealed record CreateWarehouseCommand(CreateWarehouseRequest Request) : IRequest<Result<Guid>>;

public sealed class CreateWarehouseValidator : AbstractValidator<CreateWarehouseCommand>
{
    public CreateWarehouseValidator()
    {
        RuleFor(x => x.Request.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Request.Code).NotEmpty().MaximumLength(30);
    }
}

public sealed class CreateWarehouseHandler(
    IWarehouseRepository warehouses,
    IUnitOfWork unitOfWork)
    : IRequestHandler<CreateWarehouseCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CreateWarehouseCommand request, CancellationToken ct)
    {
        var dto = request.Request;
        var code = dto.Code.Trim().ToUpperInvariant();

        if (await warehouses.CodeExistsAsync(code, ct))
            return Result.Failure<Guid>($"Bu kod ilə anbar artıq mövcuddur: {code}");

        var phone = string.IsNullOrWhiteSpace(dto.Phone) ? null : PhoneNumber.Create(dto.Phone);
        var address = WarehouseMapping.ToAddress(dto.City, dto.AddressLine);

        var warehouse = Warehouse.Create(dto.Name, dto.Code, address, phone, dto.Notes);

        await warehouses.AddAsync(warehouse, ct);
        await unitOfWork.SaveChangesAsync(ct);

        return Result.Success(warehouse.Id);
    }
}
