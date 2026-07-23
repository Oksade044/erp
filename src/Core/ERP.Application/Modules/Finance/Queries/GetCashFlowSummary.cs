using ERP.Application.Common.Interfaces;
using ERP.Application.Common.Messaging;
using ERP.Domain.ValueObjects;
using ERP.Shared.Contracts.Finance;

namespace ERP.Application.Modules.Finance.Queries;

/// <summary>Kassa/pul axını xülasəsi — ümumi mədaxil, məxaric, balans (TDD §17 — Query).</summary>
public sealed record GetCashFlowSummaryQuery : IRequest<CashFlowSummaryDto>;

public sealed class GetCashFlowSummaryHandler(IFinancialTransactionRepository transactions)
    : IRequestHandler<GetCashFlowSummaryQuery, CashFlowSummaryDto>
{
    public async Task<CashFlowSummaryDto> Handle(GetCashFlowSummaryQuery request, CancellationToken ct)
    {
        var (income, expense, count) = await transactions.GetSummaryAsync(ct);
        return new CashFlowSummaryDto(
            TotalIncome: income,
            TotalExpense: expense,
            Balance: income - expense,
            Currency: Money.DefaultCurrency,
            TransactionCount: count);
    }
}
