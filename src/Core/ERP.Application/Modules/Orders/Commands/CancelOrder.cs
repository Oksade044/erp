using ERP.Application.Common.Interfaces;
using ERP.Application.Common.Models;
using MediatR;

namespace ERP.Application.Modules.Orders.Commands;

/// <summary>Sifarişi ləğv edir (rezervi azad edir). Qaytarılmış/artıq ləğv olunmuş sifariş ləğv edilə bilməz.</summary>
public sealed record CancelOrderCommand(Guid Id) : IRequest<Result>;

public sealed class CancelOrderHandler(
    IRentalOrderRepository orders,
    IUnitOfWork unitOfWork)
    : IRequestHandler<CancelOrderCommand, Result>
{
    public async Task<Result> Handle(CancelOrderCommand request, CancellationToken ct)
    {
        var order = await orders.GetByIdWithLinesAsync(request.Id, ct);
        if (order is null)
            return Result.Failure($"Sifariş tapılmadı: {request.Id}");

        order.Cancel();
        orders.Update(order);
        await unitOfWork.SaveChangesAsync(ct);

        return Result.Success();
    }
}
