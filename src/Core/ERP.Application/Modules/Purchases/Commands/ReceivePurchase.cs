using ERP.Application.Common.Interfaces;
using ERP.Application.Common.Messaging;
using ERP.Application.Common.Models;

namespace ERP.Application.Modules.Purchases.Commands;

/// <summary>
/// Təsdiqlənmiş alışı anbara qəbul edir: statusu QəbulEdilmiş-ə keçirir VƏ hər sətir üçün
/// məhsul stokunu say qədər artırır. Alış + stok yeniləmələri tək transaction-da (TDD §15).
/// </summary>
public sealed record ReceivePurchaseCommand(Guid Id) : IRequest<Result>;

public sealed class ReceivePurchaseHandler(
    IPurchaseOrderRepository purchases,
    IProductRepository products,
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

        // Hər sətir üçün məhsul stokunu artır (mal fiziki olaraq anbara daxil oldu).
        foreach (var line in purchase.Lines)
        {
            var product = await products.GetByIdAsync(line.ProductId, ct);
            if (product is null)
                return Result.Failure($"Məhsul tapılmadı: {line.ProductId}");

            product.AdjustStock(line.Quantity);
            products.Update(product);
        }

        purchases.Update(purchase);
        await unitOfWork.SaveChangesAsync(ct);

        return Result.Success();
    }
}
