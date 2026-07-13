using ERP.Application.Common.Interfaces;
using ERP.Application.Common.Messaging;
using ERP.Application.Common.Models;

namespace ERP.Application.Modules.Orders.Commands;

/// <summary>Təsdiqlənmiş sifarişi "təhvil verilmiş" statusuna keçirir (avadanlıq müştəriyə verildi).</summary>
public sealed record DeliverOrderCommand(Guid Id) : IRequest<Result>;

public sealed class DeliverOrderHandler(
    IRentalOrderRepository orders,
    IUnitOfWork unitOfWork)
    : IRequestHandler<DeliverOrderCommand, Result>
{
    public async Task<Result> Handle(DeliverOrderCommand request, CancellationToken ct)
    {
        var order = await orders.GetByIdWithLinesAsync(request.Id, ct);
        if (order is null)
            return Result.Failure($"Sifariş tapılmadı: {request.Id}");

        order.Deliver();
        orders.Update(order);
        await unitOfWork.SaveChangesAsync(ct);

        return Result.Success();
    }
}
