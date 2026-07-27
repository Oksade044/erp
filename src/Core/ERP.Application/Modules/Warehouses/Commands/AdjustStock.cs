using ERP.Application.Common.Interfaces;
using ERP.Application.Common.Messaging;
using ERP.Application.Common.Models;
using ERP.Domain.Modules.Warehouses;
using ERP.Shared.Contracts.Warehouses;
using FluentValidation;
using ERP.Application.Modules.Warehouses;

namespace ERP.Application.Modules.Warehouses.Commands;

/// <summary>
/// Bir məhsulun bir anbardakı stokunu təyin edir (mütləq miqdar). Səviyyə yoxdursa yaradılır
/// (məhsul + anbar adları snapshot), varsa miqdar və minimum həddi yenilənir.
/// </summary>
public sealed record AdjustStockCommand(AdjustStockRequest Request) : IRequest<Result<Guid>>;

public sealed class AdjustStockValidator : AbstractValidator<AdjustStockCommand>
{
    public AdjustStockValidator()
    {
        RuleFor(x => x.Request.ProductId).NotEmpty();
        RuleFor(x => x.Request.WarehouseId).NotEmpty();
        RuleFor(x => x.Request.Quantity).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Request.MinQuantity).GreaterThanOrEqualTo(0);
    }
}

public sealed class AdjustStockHandler(
    IStockLevelRepository stockLevels,
    IProductRepository products,
    IWarehouseRepository warehouses,
    IStockNotifier notifier,
    IAuditLogWriter audit,
    ICurrentUser currentUser,
    IUnitOfWork unitOfWork)
    : IRequestHandler<AdjustStockCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(AdjustStockCommand request, CancellationToken ct)
    {
        var dto = request.Request;

        var existing = await stockLevels.GetAsync(dto.ProductId, dto.WarehouseId, ct);
        if (existing is not null)
        {
            // #43 anti-oğurluq — stok düzəlişi (əvvəl→sonra) açıq audit qeydi.
            var oldQty = existing.Quantity;
            existing.SetQuantity(dto.Quantity);
            existing.SetMinQuantity(dto.MinQuantity);
            existing.SetCondition(dto.InRepair, dto.Damaged);
            stockLevels.Update(existing);
            audit.Add(
                currentUser.FullName ?? currentUser.UserName ?? "system",
                "Stok düzəlişi",
                "StockLevel",
                existing.ProductName,
                $"{existing.ProductName} @ {existing.WarehouseName}: say {oldQty} → {dto.Quantity}" +
                $" (təmir {dto.InRepair}, zədəli {dto.Damaged})");
            await unitOfWork.SaveChangesAsync(ct);
            await notifier.NotifyStockChangedAsync(existing.ToNotification(), ct);
            return Result.Success(existing.Id);
        }

        var product = await products.GetByIdAsync(dto.ProductId, ct);
        if (product is null)
            return Result.Failure<Guid>($"Məhsul tapılmadı: {dto.ProductId}");

        var warehouse = await warehouses.GetByIdAsync(dto.WarehouseId, ct);
        if (warehouse is null)
            return Result.Failure<Guid>($"Anbar tapılmadı: {dto.WarehouseId}");

        var level = StockLevel.Create(
            product.Id, product.Name, warehouse.Id, warehouse.Name,
            dto.Quantity, dto.MinQuantity, dto.InRepair, dto.Damaged);

        await stockLevels.AddAsync(level, ct);
        await unitOfWork.SaveChangesAsync(ct);
        await notifier.NotifyStockChangedAsync(level.ToNotification(), ct);

        return Result.Success(level.Id);
    }
}
