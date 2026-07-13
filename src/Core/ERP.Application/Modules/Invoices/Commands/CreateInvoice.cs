using ERP.Application.Common.Interfaces;
using ERP.Application.Common.Messaging;
using ERP.Application.Common.Models;
using ERP.Domain.Modules.Invoices;
using ERP.Domain.Modules.Orders;
using ERP.Domain.ValueObjects;
using ERP.Shared.Contracts.Invoices;
using FluentValidation;

namespace ERP.Application.Modules.Invoices.Commands;

/// <summary>
/// Təsdiqlənmiş (və ya sonrakı statuslu) sifarişdən faktura yaradır. Sifarişin cəmi və müştəri
/// məlumatı snapshot kimi saxlanılır. Bir sifariş üçün yalnız bir faktura.
/// </summary>
public sealed record CreateInvoiceCommand(CreateInvoiceRequest Request) : IRequest<Result<Guid>>;

public sealed class CreateInvoiceValidator : AbstractValidator<CreateInvoiceCommand>
{
    public CreateInvoiceValidator() => RuleFor(x => x.Request.OrderId).NotEmpty();
}

public sealed class CreateInvoiceHandler(
    IInvoiceRepository invoices,
    IRentalOrderRepository orders,
    IUnitOfWork unitOfWork)
    : IRequestHandler<CreateInvoiceCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CreateInvoiceCommand request, CancellationToken ct)
    {
        var order = await orders.GetByIdWithLinesAsync(request.Request.OrderId, ct);
        if (order is null)
            return Result.Failure<Guid>($"Sifariş tapılmadı: {request.Request.OrderId}");

        if (order.Status is OrderStatus.Qaralama or OrderStatus.Ləğv)
            return Result.Failure<Guid>("Yalnız təsdiqlənmiş (və ya sonrakı) sifariş üçün faktura yaradıla bilər.");

        if (await invoices.ExistsForOrderAsync(order.Id, ct))
            return Result.Failure<Guid>($"Bu sifariş üçün faktura artıq mövcuddur: {order.OrderNumber}");

        var invoiceNumber = $"INV-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}";
        var total = Money.Create(order.Total.Amount, order.Total.Currency);

        var invoice = Invoice.Create(
            invoiceNumber, order.Id, order.OrderNumber,
            order.CustomerId, order.CustomerName,
            DateOnly.FromDateTime(DateTime.UtcNow), total);

        await invoices.AddAsync(invoice, ct);
        await unitOfWork.SaveChangesAsync(ct);

        return Result.Success(invoice.Id);
    }
}
