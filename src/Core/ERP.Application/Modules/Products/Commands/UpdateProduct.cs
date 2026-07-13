using ERP.Application.Common.Interfaces;
using ERP.Application.Common.Models;
using ERP.Domain.ValueObjects;
using ERP.Shared.Contracts.Products;
using FluentValidation;
using ERP.Application.Common.Messaging;

namespace ERP.Application.Modules.Products.Commands;

/// <summary>Mövcud məhsulu yeniləyir (TDD §17 — Command).</summary>
public sealed record UpdateProductCommand(Guid Id, UpdateProductRequest Request) : IRequest<Result>;

public sealed class UpdateProductValidator : AbstractValidator<UpdateProductCommand>
{
    public UpdateProductValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Request.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Request.TrackingMode).NotEmpty();
        RuleFor(x => x.Request.RentalPrice).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Request.StockQuantity).GreaterThanOrEqualTo(0);
    }
}

public sealed class UpdateProductHandler(
    IProductRepository products,
    IUnitOfWork unitOfWork)
    : IRequestHandler<UpdateProductCommand, Result>
{
    public async Task<Result> Handle(UpdateProductCommand request, CancellationToken ct)
    {
        var product = await products.GetByIdAsync(request.Id, ct);
        if (product is null)
            return Result.Failure($"Məhsul tapılmadı: {request.Id}");

        var dto = request.Request;

        product.Rename(dto.Name);
        product.ChangePrice(Money.Create(dto.RentalPrice, dto.Currency));
        product.ChangeTrackingMode(ProductMapping.ParseTrackingMode(dto.TrackingMode));
        product.SetStock(dto.StockQuantity);
        product.ChangeCategory(dto.Category);
        product.ChangeDescription(dto.Description);
        if (dto.IsActive) product.Activate(); else product.Deactivate();

        products.Update(product);
        await unitOfWork.SaveChangesAsync(ct);

        return Result.Success();
    }
}
