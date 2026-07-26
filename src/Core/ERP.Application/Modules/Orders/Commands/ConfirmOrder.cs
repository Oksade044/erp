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
    IInvoiceRepository invoices,
    IAvailabilityReader availability,
    ICurrentUser currentUser,
    IUnitOfWork unitOfWork)
    : IRequestHandler<ConfirmOrderCommand, Result>
{
    public async Task<Result> Handle(ConfirmOrderCommand request, CancellationToken ct)
    {
        var order = await orders.GetByIdWithLinesAsync(request.Id, ct);
        if (order is null)
            return Result.Failure($"Sifariş tapılmadı: {request.Id}");

        // #27 — mövcudluq yoxlaması: yalnız BOŞ (rezerv/kirayə/təmir/zədəli xaric) stoka icazə.
        // Sifariş hələ Qaralama olduğundan onun sətirləri boş sayına daxil deyil.
        foreach (var line in order.Lines)
        {
            var avail = await availability.GetProductAvailabilityAsync(line.ProductId, ct);

            int free;
            string where;
            if (line.WarehouseId is { } whId)
            {
                var wh = avail.FirstOrDefault(a => a.WarehouseId == whId);
                free = wh?.Free ?? 0;
                where = $"'{line.WarehouseName ?? "anbar"}' anbarında";
            }
            else
            {
                free = avail.Sum(a => a.Free);
                where = "boş";
            }

            if (line.Quantity > free)
                return Result.Failure(
                    $"'{line.ProductName}' üçün {where} boş yalnız {free} ədəddir, " +
                    $"tələb {line.Quantity}. Rezervdə/kirayədə olan stok sifariş edilə bilməz.");
        }

        // Sifariş izlənilir (GetByIdWithLinesAsync) → mutasiya kifayətdir; Update() çağırsaq
        // EF bütün sahələri Modified sayar və audit tarixçəsi lüzumsuz dolğun olar.
        order.Confirm(currentUser.FullName ?? currentUser.UserName);

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
