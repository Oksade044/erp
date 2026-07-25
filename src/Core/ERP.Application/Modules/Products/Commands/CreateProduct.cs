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
    IWarehouseRepository warehouses,
    IStockLevelRepository stockLevels,
    ICategoryRepository categories,
    IUnitOfWork unitOfWork)
    : IRequestHandler<CreateProductCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CreateProductCommand request, CancellationToken ct)
    {
        var dto = request.Request;

        // Anbar seçilibsə, mövcudluğunu əvvəlcədən yoxla (ilkin stok ora yazılacaq).
        Domain.Modules.Warehouses.Warehouse? warehouse = null;
        if (dto.WarehouseId is { } wid)
        {
            warehouse = await warehouses.GetByIdAsync(wid, ct);
            if (warehouse is null)
                return Result.Failure<Guid>($"Anbar tapılmadı: {wid}");
        }

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

        // Yeni kateqoriya adı yazılıbsa, kateqoriya lüğətinə əlavə et (mövcud deyilsə).
        await EnsureCategoryAsync(dto.Category, ct);

        // Anbar seçilibsə, ilkin stoku o anbarın StockLevel-inə yaz (bir tranzaksiyada).
        if (warehouse is not null)
        {
            var level = Domain.Modules.Warehouses.StockLevel.Create(
                product.Id, product.Name, warehouse.Id, warehouse.Name,
                dto.StockQuantity, dto.MinStockQuantity);
            await stockLevels.AddAsync(level, ct);
        }

        await unitOfWork.SaveChangesAsync(ct);

        return Result.Success(product.Id);
    }

    private async Task EnsureCategoryAsync(string? categoryName, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(categoryName)) return;
        if (await categories.GetByNameAsync(categoryName, ct) is null)
            await categories.AddAsync(Category.Create(categoryName), ct);
    }
}
