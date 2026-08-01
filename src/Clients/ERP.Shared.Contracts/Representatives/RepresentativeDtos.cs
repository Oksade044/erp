namespace ERP.Shared.Contracts.Representatives;

/// <summary>Təmsilçi defter qeydi (#16-18).</summary>
public sealed record RepresentativeEntryDto(
    Guid Id,
    string RepresentativeName,
    DateOnly Date,
    string Type,
    decimal Amount,
    decimal SignedAmount,
    string Currency,
    string? Description,
    string? OrderNumber);

/// <summary>Təmsilçi balans xülasəsi — cari borc + qeydlər.</summary>
public sealed record RepresentativeLedgerDto(
    string RepresentativeName,
    decimal Balance,          // mənfi = borclu (sifariş yaratmalı), müsbət = artıq
    decimal TotalDebt,
    decimal TotalOrders,
    string Currency,
    IReadOnlyList<RepresentativeEntryDto> Entries);

/// <summary>Siyahı üçün təmsilçi balans sətri.</summary>
public sealed record RepresentativeBalanceDto(
    string RepresentativeName,
    decimal Balance,
    string Currency);

/// <summary>Admin təmsilçiyə borc təyin edir.</summary>
public sealed record AssignDebtRequest(
    string RepresentativeName,
    decimal Amount,
    DateOnly Date,
    string? Description);
