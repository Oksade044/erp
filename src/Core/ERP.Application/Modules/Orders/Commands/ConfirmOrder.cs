using ERP.Application.Common.Interfaces;
using ERP.Application.Common.Models;
using ERP.Application.Common.Messaging;
using ERP.Domain.Modules.Invoices;
using ERP.Domain.ValueObjects;

namespace ERP.Application.Modules.Orders.Commands;

/// <summary>
/// Sifarişi təsdiqləyir. Təsdiqdən əvvəl hər məhsul üçün MÖVCUDLUQ yoxlanılır:
/// üst-üstə düşən digər aktiv sifarişlərdə rezerv + bu sifariş ≤ anbar sayı.
/// İkiqat-bronun qarşısını alır (TDD §38). Təsdiqlə eyni anda FAKTURA avtomatik yaradılır (#22).
/// </summary>
public sealed record ConfirmOrderCommand(Guid Id) : IRequest<Result>;

public sealed class ConfirmOrderHandler(
    IRentalOrderRepository orders,
    IProductRepository products,
    IInvoiceRepository invoices,
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

        // #22 — təsdiq zamanı faktura avtomatik yaradılır (əvvəldən yoxdursa).
        if (!await invoices.ExistsForOrderAsync(order.Id, ct))
        {
            var invoiceNumber = $"INV-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}";
            var total = Money.Create(order.Total.Amount, order.Total.Currency);
            var invoice = Invoice.Create(
                invoiceNumber, order.Id, order.OrderNumber,
                order.CustomerId, order.CustomerName,
                DateOnly.FromDateTime(DateTime.UtcNow), total);
            await invoices.AddAsync(invoice, ct);
        }

        await unitOfWork.SaveChangesAsync(ct);

        return Result.Success();
    }
}
