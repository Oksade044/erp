using ERP.Application.Common.Interfaces;
using ERP.Application.Common.Messaging;
using ERP.Application.Common.Models;
using ERP.Domain.Modules.Purchases;
using ERP.Domain.ValueObjects;
using ERP.Shared.Contracts.Purchases;
using FluentValidation;

namespace ERP.Application.Modules.Purchases.Commands;

/// <summary>
/// Yeni alış sifarişi yaradır (Qaralama statusunda). Təchizatçı adı və məhsul adları
/// snapshot kimi saxlanılır; alış qiyməti (UnitCost) sətir səviyyəsində qeyd olunur.
/// </summary>
public sealed record CreatePurchaseCommand(CreatePurchaseRequest Request) : IRequest<Result<Guid>>;

public sealed class CreatePurchaseValidator : AbstractValidator<CreatePurchaseCommand>
{
    public CreatePurchaseValidator()
    {
        RuleFor(x => x.Request.SupplierId).NotEmpty();
        RuleFor(x => x.Request.Lines).NotEmpty().WithMessage("Alışda ən azı bir sətir olmalıdır.");
        RuleForEach(x => x.Request.Lines).ChildRules(line =>
        {
            line.RuleFor(l => l.ProductId).NotEmpty();
            line.RuleFor(l => l.Quantity).GreaterThan(0);
            line.RuleFor(l => l.UnitCost).GreaterThanOrEqualTo(0);
        });
    }
}

public sealed class CreatePurchaseHandler(
    IPurchaseOrderRepository purchases,
    ISupplierRepository suppliers,
    IProductRepository products,
    IUnitOfWork unitOfWork)
    : IRequestHandler<CreatePurchaseCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CreatePurchaseCommand request, CancellationToken ct)
    {
        var dto = request.Request;

        var supplier = await suppliers.GetByIdAsync(dto.SupplierId, ct);
        if (supplier is null)
            return Result.Failure<Guid>($"Təchizatçı tapılmadı: {dto.SupplierId}");

        var number = $"ALS-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}";
        var purchase = PurchaseOrder.Create(number, supplier.Id, supplier.Name, dto.OrderDate, dto.Notes, dto.WarehouseId);

        foreach (var lineDto in dto.Lines)
        {
            var product = await products.GetByIdAsync(lineDto.ProductId, ct);
            if (product is null)
                return Result.Failure<Guid>($"Məhsul tapılmadı: {lineDto.ProductId}");

            var unitCost = Money.Create(lineDto.UnitCost);
            purchase.AddLine(product.Id, product.Name, lineDto.Quantity, unitCost);
        }

        await purchases.AddAsync(purchase, ct);
        await unitOfWork.SaveChangesAsync(ct);

        return Result.Success(purchase.Id);
    }
}
