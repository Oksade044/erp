using ERP.Application.Common.Interfaces;
using ERP.Application.Common.Messaging;
using ERP.Application.Common.Models;

namespace ERP.Application.Modules.Purchases.Commands;

/// <summary>Qaralama alışı təsdiqləyir (təchizatçıya sifariş verildi).</summary>
public sealed record ConfirmPurchaseCommand(Guid Id) : IRequest<Result>;

public sealed class ConfirmPurchaseHandler(
    IPurchaseOrderRepository purchases,
    IUnitOfWork unitOfWork)
    : IRequestHandler<ConfirmPurchaseCommand, Result>
{
    public async Task<Result> Handle(ConfirmPurchaseCommand request, CancellationToken ct)
    {
        var purchase = await purchases.GetByIdWithLinesAsync(request.Id, ct);
        if (purchase is null)
            return Result.Failure($"Alış tapılmadı: {request.Id}");

        purchase.Confirm();
        purchases.Update(purchase);
        await unitOfWork.SaveChangesAsync(ct);

        return Result.Success();
    }
}
