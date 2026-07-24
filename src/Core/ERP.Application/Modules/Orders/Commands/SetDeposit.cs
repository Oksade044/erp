using ERP.Application.Common.Interfaces;
using ERP.Application.Common.Messaging;
using ERP.Application.Common.Models;
using ERP.Domain.ValueObjects;
using ERP.Shared.Contracts.Orders;
using FluentValidation;

namespace ERP.Application.Modules.Orders.Commands;

/// <summary>Sifarişə depozit/girov məbləğini təyin edir (TDD §17 — Command).</summary>
public sealed record SetDepositCommand(Guid Id, SetDepositRequest Request) : IRequest<Result>;

public sealed class SetDepositValidator : AbstractValidator<SetDepositCommand>
{
    public SetDepositValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Request.Deposit).GreaterThanOrEqualTo(0);
    }
}

public sealed class SetDepositHandler(
    IRentalOrderRepository orders,
    IUnitOfWork unitOfWork)
    : IRequestHandler<SetDepositCommand, Result>
{
    public async Task<Result> Handle(SetDepositCommand request, CancellationToken ct)
    {
        var order = await orders.GetByIdWithLinesAsync(request.Id, ct);
        if (order is null)
            return Result.Failure($"Sifariş tapılmadı: {request.Id}");

        try
        {
            order.SetDeposit(Money.Create(request.Request.Deposit));
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
