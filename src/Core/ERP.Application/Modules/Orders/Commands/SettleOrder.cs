using ERP.Application.Common.Interfaces;
using ERP.Application.Common.Messaging;
using ERP.Application.Common.Models;
using ERP.Domain.ValueObjects;
using ERP.Shared.Contracts.Orders;
using FluentValidation;

namespace ERP.Application.Modules.Orders.Commands;

/// <summary>
/// Qaytarma hesablaşması: zədə/itki dəyəri və cəriməni qeyd edir; geri qaytarılacaq depozit
/// avtomatik hesablanır (TDD §17 — Command).
/// </summary>
public sealed record SettleOrderCommand(Guid Id, SettleOrderRequest Request) : IRequest<Result>;

public sealed class SettleOrderValidator : AbstractValidator<SettleOrderCommand>
{
    public SettleOrderValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Request.DamageCharge).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Request.PenaltyCharge).GreaterThanOrEqualTo(0);
    }
}

public sealed class SettleOrderHandler(
    IRentalOrderRepository orders,
    IInvoiceRepository invoices,
    IAuditLogWriter audit,
    ICurrentUser currentUser,
    IUnitOfWork unitOfWork)
    : IRequestHandler<SettleOrderCommand, Result>
{
    public async Task<Result> Handle(SettleOrderCommand request, CancellationToken ct)
    {
        var order = await orders.GetByIdWithLinesAsync(request.Id, ct);
        if (order is null)
            return Result.Failure($"Sifariş tapılmadı: {request.Id}");

        try
        {
            order.Settle(
                Money.Create(request.Request.DamageCharge),
                Money.Create(request.Request.PenaltyCharge),
                request.Request.Notes);
        }
        catch (ERP.Domain.Exceptions.DomainException ex)
        {
            return Result.Failure(ex.Message);
        }

        // #C — zədə/cərimə fakturaya yansısın (yekun borc artır).
        var invoice = await invoices.GetByOrderIdAsync(order.Id, ct);
        if (invoice is not null)
            invoice.SetAdditionalCharges(order.TotalCharges);

        // #43 — depozit/hesablaşma əməliyyatını audit jurnalına yaz.
        audit.Add(
            currentUser.FullName ?? currentUser.UserName ?? "system",
            "Hesablaşma (zədə/cərimə)",
            "Order",
            order.OrderNumber,
            $"Zədə {order.DamageCharge?.Amount ?? 0:0.00}, cərimə {order.PenaltyCharge?.Amount ?? 0:0.00}, " +
            $"depozit qaytarma {order.DepositRefund.Amount:0.00} (sifariş {order.OrderNumber})");

        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success();
    }
}
