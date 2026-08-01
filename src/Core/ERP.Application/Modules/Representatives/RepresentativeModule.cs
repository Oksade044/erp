using ERP.Application.Common.Interfaces;
using ERP.Application.Common.Messaging;
using ERP.Application.Common.Models;
using ERP.Domain.Exceptions;
using ERP.Domain.Modules.Representatives;
using ERP.Domain.ValueObjects;
using ERP.Shared.Contracts.Representatives;
using FluentValidation;

namespace ERP.Application.Modules.Representatives;

public static class RepresentativeMapping
{
    public static RepresentativeEntryDto ToDto(this RepresentativeEntry e) => new(
        Id: e.Id,
        RepresentativeName: e.RepresentativeName,
        Date: e.Date,
        Type: e.Type.ToString(),
        Amount: e.Amount.Amount,
        SignedAmount: e.SignedAmount,
        Currency: e.Amount.Currency,
        Description: e.Description,
        OrderNumber: e.OrderNumber);
}

// --- Admin təmsilçiyə borc təyin edir (#16) ---
public sealed record AssignDebtCommand(AssignDebtRequest Request) : IRequest<Result>;

public sealed class AssignDebtValidator : AbstractValidator<AssignDebtCommand>
{
    public AssignDebtValidator()
    {
        RuleFor(x => x.Request.RepresentativeName).NotEmpty();
        RuleFor(x => x.Request.Amount).GreaterThan(0);
    }
}

public sealed class AssignDebtHandler(IRepresentativeRepository repo, IUnitOfWork uow)
    : IRequestHandler<AssignDebtCommand, Result>
{
    public async Task<Result> Handle(AssignDebtCommand request, CancellationToken ct)
    {
        var dto = request.Request;
        try
        {
            var entry = RepresentativeEntry.Create(
                dto.RepresentativeName, dto.Date, RepEntryType.Borc,
                Money.Create(dto.Amount), dto.Description);
            await repo.AddAsync(entry, ct);
            await uow.SaveChangesAsync(ct);
            return Result.Success();
        }
        catch (DomainException ex) { return Result.Failure(ex.Message); }
    }
}

// --- Bir təmsilçinin defteri (balans + qeydlər) ---
public sealed record GetRepresentativeLedgerQuery(string Name) : IRequest<Result<RepresentativeLedgerDto>>;

public sealed class GetRepresentativeLedgerHandler(IRepresentativeRepository repo)
    : IRequestHandler<GetRepresentativeLedgerQuery, Result<RepresentativeLedgerDto>>
{
    public async Task<Result<RepresentativeLedgerDto>> Handle(GetRepresentativeLedgerQuery request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return Result.Failure<RepresentativeLedgerDto>("Təmsilçi tələb olunur.");

        var entries = await repo.GetByRepresentativeAsync(request.Name, ct);
        var totalDebt = entries.Where(e => e.Type is RepEntryType.Borc or RepEntryType.SifarişLəğvi).Sum(e => e.Amount.Amount);
        var totalOrders = entries.Where(e => e.Type is RepEntryType.Sifariş or RepEntryType.Ödəniş).Sum(e => e.Amount.Amount);

        return Result.Success(new RepresentativeLedgerDto(
            RepresentativeName: request.Name,
            Balance: entries.Sum(e => e.SignedAmount),
            TotalDebt: totalDebt,
            TotalOrders: totalOrders,
            Currency: "AZN",
            Entries: entries.Select(e => e.ToDto()).ToList()));
    }
}

// --- Bütün təmsilçilərin balansı (siyahı) ---
public sealed record GetRepresentativeBalancesQuery : IRequest<IReadOnlyList<RepresentativeBalanceDto>>;

public sealed class GetRepresentativeBalancesHandler(IRepresentativeRepository repo)
    : IRequestHandler<GetRepresentativeBalancesQuery, IReadOnlyList<RepresentativeBalanceDto>>
{
    public async Task<IReadOnlyList<RepresentativeBalanceDto>> Handle(GetRepresentativeBalancesQuery request, CancellationToken ct)
    {
        var all = await repo.GetAllAsync(ct);
        return all
            .GroupBy(e => e.RepresentativeName)
            .Select(g => new RepresentativeBalanceDto(g.Key, g.Sum(e => e.SignedAmount), "AZN"))
            .OrderBy(r => r.Balance)
            .ToList();
    }
}
