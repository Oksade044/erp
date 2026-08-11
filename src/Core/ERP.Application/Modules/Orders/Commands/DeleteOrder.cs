using ERP.Application.Common.Interfaces;
using ERP.Application.Common.Models;
using ERP.Application.Common.Messaging;
using ERP.Domain.Modules.Orders;

namespace ERP.Application.Modules.Orders.Commands;

/// <summary>
/// Sifarişi silir (soft delete). Yalnız QARALAMA və ya LƏĞV edilmiş sifariş silinə bilər —
/// bunların stok/maliyyə təsiri yoxdur (rezerv ləğvdə azad olunub). Təsdiqlənmiş/təhvil verilmiş/
/// qaytarılmış sifarişlərin izi qalmalıdır (audit/rezerv/maliyyə).
/// </summary>
public sealed record DeleteOrderCommand(Guid Id) : IRequest<Result>;

public sealed class DeleteOrderHandler(
    IRentalOrderRepository orders,
    IUnitOfWork unitOfWork)
    : IRequestHandler<DeleteOrderCommand, Result>
{
    public async Task<Result> Handle(DeleteOrderCommand request, CancellationToken ct)
    {
        var order = await orders.GetByIdWithLinesAsync(request.Id, ct);
        if (order is null)
            return Result.Failure($"Sifariş tapılmadı: {request.Id}");

        if (order.Status is not (OrderStatus.Qaralama or OrderStatus.Ləğv))
            return Result.Failure("Yalnız qaralama və ya ləğv edilmiş sifariş silinə bilər.");

        orders.Remove(order);
        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success();
    }
}
