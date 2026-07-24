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

        orders.Update(order);
        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success();
    }
}
