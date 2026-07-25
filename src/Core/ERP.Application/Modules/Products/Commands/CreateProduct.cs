using ERP.Application.Common.Interfaces;
using ERP.Application.Common.Models;
using ERP.Domain.Modules.Products;
using ERP.Domain.ValueObjects;
using ERP.Shared.Contracts.Products;
using FluentValidation;
using ERP.Application.Common.Messaging;

namespace ERP.Application.Modules.Products.Commands;

/// <summary>Yeni məhsul yaradır (TDD §17 — Command). Uğurda məhsul Id-sini qaytarır.</summary>
public sealed record CreateProductCommand(CreateProductRequest Request) : IRequest<Result<Guid>>;

public sealed class CreateProductValidator : AbstractValidator<CreateProductCommand>
{
    public CreateProductValidator()
    {
        // SKU opsionaldır — boş olsa server avtomatik generasiya edir.
        RuleFor(x => x.Request.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Request.TrackingMode).NotEmpty();
        RuleFor(x => x.Request.RentalPrice).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Request.StockQuantity).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Request.MinStockQuantity).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Request.PurchasePrice).GreaterThanOrEqualTo(0).When(x => x.Request.PurchasePrice.HasValue);
        RuleFor(x => x.Request.SalePrice).GreaterThanOrEqualTo(0).When(x => x.Request.SalePrice.HasValue);
    }
}

public sealed class CreateProductHandler(
    IProductRepository products,
    IUnitOfWork unitOfWork)
    : IRequestHandler<CreateProductCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CreateProductCommand request, CancellationToken ct)
    {
        var dto = request.Request;

        // SKU verilməyibsə avtomatik generasiya et (PRD-000001); verilibsə normalizə + unikallıq yoxla.
        var skuRaw = string.IsNullOrWhiteSpace(dto.Sku)
            ? await products.GenerateNextSkuAsync(ct)
            : dto.Sku;
        var sku = Sku.Create(skuRaw);
        if (await products.SkuExistsAsync(sku.Value, ct))
            return Result.Failure<Guid>($"Bu SKU ilə məhsul artıq mövcuddur: {sku.Value}");

        var mode = ProductMapping.ParseTrackingMode(dto.TrackingMode);
        var price = Money.Create(dto.RentalPrice, dto.Currency);
        var purchase = dto.PurchasePrice.HasValue ? Money.Create(dto.PurchasePrice.Value, dto.Currency) : null;
        var sale = dto.SalePrice.HasValue ? Money.Create(dto.SalePrice.Value, dto.Currency) : null;

        var product = Product.Create(sku, dto.Name, price, mode, dto.StockQuantity,
            dto.Category, dto.Description, purchase, sale, dto.MinStockQuantity);

        await products.AddAsync(product, ct);
        await unitOfWork.SaveChangesAsync(ct);

        return Result.Success(product.Id);
    }
}
