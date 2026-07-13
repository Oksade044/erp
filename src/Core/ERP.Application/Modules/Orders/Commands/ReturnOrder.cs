using ERP.Application.Common.Interfaces;
using ERP.Application.Common.Messaging;
using ERP.Application.Common.Models;

namespace ERP.Application.Modules.Orders.Commands;

/// <summary>Təhvil verilmiş sifarişi "qaytarılmış" statusuna keçirir (avadanlıq geri alındı → rezerv azad olur).</summary>
public sealed record ReturnOrderCommand(Guid Id) : IRequest<Result>;

public sealed class ReturnOrderHandler(
    IRentalOrderRepository orders,
    IUnitOfWork unitOfWork)
    : IRequestHandler<ReturnOrderCommand, Result>
{
    public async Task<Result> Handle(ReturnOrderCommand request, CancellationToken ct)
    {
        var order = await orders.GetByIdWithLinesAsync(request.Id, ct);
        if (order is null)
            return Result.Failure($"Sifariş tapılmadı: {request.Id}");

        order.Return();
        orders.Update(order);
        await unitOfWork.SaveChangesAsync(ct);

        return Result.Success();
    }
}
