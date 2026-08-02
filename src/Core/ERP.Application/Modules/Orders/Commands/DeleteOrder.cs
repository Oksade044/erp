using ERP.Application.Common.Interfaces;
using ERP.Application.Common.Models;
using ERP.Application.Common.Messaging;
using ERP.Domain.Modules.Orders;

namespace ERP.Application.Modules.Orders.Commands;

/// <summary>
/// Sifarişi silir (soft delete). Yalnız QARALAMA sifariş silinə bilər — təsdiqlənmiş/təhvil
/// verilmiş sifarişlərin izi qalmalıdır (audit/rezerv). Qaralama düzənləmə üçün istifadə olunur.
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

        if (order.Status != OrderStatus.Qaralama)
            return Result.Failure("Yalnız qaralama sifariş silinə bilər.");

        orders.Remove(order);
        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success();
    }
}
