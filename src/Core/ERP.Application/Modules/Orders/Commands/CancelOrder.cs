using ERP.Application.Common.Interfaces;
using ERP.Application.Common.Models;
using ERP.Application.Common.Messaging;

namespace ERP.Application.Modules.Orders.Commands;

/// <summary>Sifarişi ləğv edir (rezervi azad edir). Qaytarılmış/artıq ləğv olunmuş sifariş ləğv edilə bilməz.</summary>
public sealed record CancelOrderCommand(Guid Id) : IRequest<Result>;

public sealed class CancelOrderHandler(
    IRentalOrderRepository orders,
    IRepresentativeRepository representatives,
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

        // #18 — sifariş ləğv olundu: təmsilçiyə verilmiş krediti geri al (borc bərpa olunur).
        if (!string.IsNullOrWhiteSpace(order.CreatedByName) && order.Total.Amount > 0)
        {
            var reversal = ERP.Domain.Modules.Representatives.RepresentativeEntry.Create(
                order.CreatedByName!, DateOnly.FromDateTime(DateTime.UtcNow),
                ERP.Domain.Modules.Representatives.RepEntryType.SifarişLəğvi,
                order.Total, $"Ləğv: {order.OrderNumber}", order.OrderNumber);
            await representatives.AddAsync(reversal, ct);
        }

        await unitOfWork.SaveChangesAsync(ct);

        return Result.Success();
    }
}
