using ERP.Application.Common.Interfaces;
using ERP.Application.Common.Models;
using ERP.Domain.Modules.Products;
using ERP.Domain.ValueObjects;
using ERP.Shared.Contracts.Products;
using FluentValidation;
using MediatR;

namespace ERP.Application.Modules.Products.Commands;

/// <summary>Yeni məhsul yaradır (TDD §17 — Command). Uğurda məhsul Id-sini qaytarır.</summary>
public sealed record CreateProductCommand(CreateProductRequest Request) : IRequest<Result<Guid>>;

public sealed class CreateProductValidator : AbstractValidator<CreateProductCommand>
{
    public CreateProductValidator()
    {
        RuleFor(x => x.Request.Sku).NotEmpty();
        RuleFor(x => x.Request.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Request.TrackingMode).NotEmpty();
        RuleFor(x => x.Request.RentalPrice).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Request.StockQuantity).GreaterThanOrEqualTo(0);
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

        var sku = Sku.Create(dto.Sku);
        if (await products.SkuExistsAsync(sku.Value, ct))
            return Result.Failure<Guid>($"Bu SKU ilə məhsul artıq mövcuddur: {sku.Value}");

        var mode = ProductMapping.ParseTrackingMode(dto.TrackingMode);
        var price = Money.Create(dto.RentalPrice, dto.Currency);

        var product = Product.Create(sku, dto.Name, price, mode, dto.StockQuantity, dto.Category, dto.Description);

        await products.AddAsync(product, ct);
        await unitOfWork.SaveChangesAsync(ct);

        return Result.Success(product.Id);
    }
}
