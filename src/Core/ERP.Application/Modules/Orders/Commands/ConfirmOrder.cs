using ERP.Application.Common.Interfaces;
using ERP.Application.Common.Models;
using ERP.Application.Common.Messaging;

namespace ERP.Application.Modules.Orders.Commands;

/// <summary>
/// Sifarişi təsdiqləyir. Təsdiqdən əvvəl hər məhsul üçün MÖVCUDLUQ yoxlanılır:
/// üst-üstə düşən digər aktiv sifarişlərdə rezerv + bu sifariş ≤ anbar sayı.
/// İkiqat-bronun qarşısını alır (TDD §38).
/// </summary>
public sealed record ConfirmOrderCommand(Guid Id) : IRequest<Result>;

public sealed class ConfirmOrderHandler(
    IRentalOrderRepository orders,
    IProductRepository products,
    IUnitOfWork unitOfWork)
    : IRequestHandler<ConfirmOrderCommand, Result>
{
    public async Task<Result> Handle(ConfirmOrderCommand request, CancellationToken ct)
    {
        var order = await orders.GetByIdWithLinesAsync(request.Id, ct);
        if (order is null)
            return Result.Failure($"Sifariş tapılmadı: {request.Id}");

        // Mövcudluq yoxlaması — hər sətir üçün.
        foreach (var line in order.Lines)
        {
            var product = await products.GetByIdAsync(line.ProductId, ct);
            if (product is null)
                return Result.Failure($"Məhsul tapılmadı: {line.ProductName}");

            var reserved = await orders.GetReservedQuantityAsync(
                line.ProductId, order.StartDate, order.EndDate, order.Id, ct);

            var available = product.StockQuantity - reserved;
            if (line.Quantity > available)
                return Result.Failure(
                    $"'{product.Name}' üçün kifayət qədər mövcud deyil: tələb {line.Quantity}, " +
                    $"mövcud {available} (anbar {product.StockQuantity}, rezerv {reserved}) " +
                    $"[{order.StartDate:dd.MM.yyyy}–{order.EndDate:dd.MM.yyyy}].");
        }

        order.Confirm();
        orders.Update(order);
        await unitOfWork.SaveChangesAsync(ct);

        return Result.Success();
    }
}
