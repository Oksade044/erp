using ERP.Application.Common.Interfaces;
using ERP.Application.Common.Models;
using ERP.Domain.Modules.Products;
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
        RuleFor(x => x.Request.MinStockQuantity).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Request.PurchasePrice).GreaterThanOrEqualTo(0).When(x => x.Request.PurchasePrice.HasValue);
        RuleFor(x => x.Request.SalePrice).GreaterThanOrEqualTo(0).When(x => x.Request.SalePrice.HasValue);
    }
}

public sealed class UpdateProductHandler(
    IProductRepository products,
    ICategoryRepository categories,
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
        product.ChangePurchasePrice(dto.PurchasePrice.HasValue ? Money.Create(dto.PurchasePrice.Value, dto.Currency) : null);
        product.ChangeSalePrice(dto.SalePrice.HasValue ? Money.Create(dto.SalePrice.Value, dto.Currency) : null);
        product.ChangeTrackingMode(ProductMapping.ParseTrackingMode(dto.TrackingMode));
        product.SetStock(dto.StockQuantity);
        product.SetMinStock(dto.MinStockQuantity);
        product.ChangeCategory(dto.Category);
        product.ChangeDescription(dto.Description);
        product.ChangeUnit(dto.Unit);
        if (dto.IsActive) product.Activate(); else product.Deactivate();

        // Yeni kateqoriya adı yazılıbsa lüğətə əlavə et.
        if (!string.IsNullOrWhiteSpace(dto.Category) && await categories.GetByNameAsync(dto.Category, ct) is null)
            await categories.AddAsync(Category.Create(dto.Category), ct);

        products.Update(product);
        await unitOfWork.SaveChangesAsync(ct);

        return Result.Success();
    }
}
