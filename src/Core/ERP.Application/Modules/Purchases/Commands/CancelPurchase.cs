using ERP.Application.Common.Interfaces;
using ERP.Application.Common.Messaging;
using ERP.Application.Common.Models;

namespace ERP.Application.Modules.Purchases.Commands;

/// <summary>Alışı ləğv edir (qəbul edilmiş alış ləğv edilə bilməz).</summary>
public sealed record CancelPurchaseCommand(Guid Id) : IRequest<Result>;

public sealed class CancelPurchaseHandler(
    IPurchaseOrderRepository purchases,
    IUnitOfWork unitOfWork)
    : IRequestHandler<CancelPurchaseCommand, Result>
{
    public async Task<Result> Handle(CancelPurchaseCommand request, CancellationToken ct)
    {
        var purchase = await purchases.GetByIdWithLinesAsync(request.Id, ct);
        if (purchase is null)
            return Result.Failure($"Alış tapılmadı: {request.Id}");

        purchase.Cancel();
        purchases.Update(purchase);
        await unitOfWork.SaveChangesAsync(ct);

        return Result.Success();
    }
}
