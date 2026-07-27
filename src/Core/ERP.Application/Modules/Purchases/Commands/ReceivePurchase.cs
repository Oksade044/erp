using ERP.Application.Common.Interfaces;
using ERP.Application.Common.Messaging;
using ERP.Application.Common.Models;
using ERP.Application.Modules.Warehouses;
using ERP.Domain.Modules.Warehouses;

namespace ERP.Application.Modules.Purchases.Commands;

/// <summary>
/// Təsdiqlənmiş alışı anbara qəbul edir: statusu QəbulEdilmiş-ə keçirir VƏ hər sətir üçün
/// stoku say qədər artırır. Alışda anbar göstərilibsə mal həmin anbarın StockLevel-inə
/// yazılır (icarə/anbar uçotunda görünür); məhsulun ümumi StockQuantity də artırılır.
/// Bütün yeniləmələr tək transaction-da (TDD §15).
/// </summary>
public sealed record ReceivePurchaseCommand(Guid Id) : IRequest<Result>;

public sealed class ReceivePurchaseHandler(
    IPurchaseOrderRepository purchases,
    IProductRepository products,
    IStockLevelRepository stockLevels,
    IWarehouseRepository warehouses,
    IStockNotifier notifier,
    IUnitOfWork unitOfWork)
    : IRequestHandler<ReceivePurchaseCommand, Result>
{
    public async Task<Result> Handle(ReceivePurchaseCommand request, CancellationToken ct)
    {
        var purchase = await purchases.GetByIdWithLinesAsync(request.Id, ct);
        if (purchase is null)
            return Result.Failure($"Alış tapılmadı: {request.Id}");

        // Status keçidi (invariant yoxlaması) — stok artımından əvvəl.
        purchase.Receive();

        // Anbar göstərilibsə StockLevel-ə yazmaq üçün onu bir dəfə oxu.
        Warehouse? warehouse = null;
        if (purchase.WarehouseId is { } whId)
        {
            warehouse = await warehouses.GetByIdAsync(whId, ct);
            if (warehouse is null)
                return Result.Failure($"Anbar tapılmadı: {whId}");
        }

        foreach (var line in purchase.Lines)
        {
            var product = await products.GetByIdAsync(line.ProductId, ct);
            if (product is null)
                return Result.Failure($"Məhsul tapılmadı: {line.ProductId}");

            // Məhsulun ümumi stok sayğacı (köhnə uçot, geriyə uyğunluq).
            product.AdjustStock(line.Quantity);
            products.Update(product);

            // Anbar göstərilibsə həmin anbarın StockLevel-ini artır (çox-anbar uçotu).
            if (warehouse is not null)
            {
                var level = await stockLevels.GetAsync(product.Id, warehouse.Id, ct);
                if (level is null)
                {
                    level = StockLevel.Create(product.Id, product.Name, warehouse.Id, warehouse.Name, line.Quantity);
                    await stockLevels.AddAsync(level, ct);
                }
                else
                {
                    level.Increase(line.Quantity);
                    stockLevels.Update(level);
                }
            }
        }

        purchases.Update(purchase);
        await unitOfWork.SaveChangesAsync(ct);

        // Anbara yazıldısa canlı stok bildirişi (SignalR).
        if (warehouse is not null)
            foreach (var line in purchase.Lines)
            {
                var level = await stockLevels.GetAsync(line.ProductId, warehouse.Id, ct);
                if (level is not null) await notifier.NotifyStockChangedAsync(level.ToNotification(), ct);
            }

        return Result.Success();
    }
}
