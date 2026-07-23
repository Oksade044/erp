namespace ERP.Shared.Contracts.Finance;

/// <summary>Kassa/pul axını xülasəsi — ümumi mədaxil, məxaric və balans (kassa qalığı).</summary>
public sealed record CashFlowSummaryDto(
    decimal TotalIncome,
    decimal TotalExpense,
    decimal Balance,
    string Currency,
    int TransactionCount);
