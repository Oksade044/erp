namespace ERP.Shared.Contracts.Finance;

/// <summary>Maliyyə əməliyyatı cavab DTO-su (TDD §12).</summary>
public sealed record TransactionDto(
    Guid Id,
    string TransactionNumber,
    string Type,
    string Category,
    decimal Amount,
    string Currency,
    DateOnly Date,
    string Method,
    string? Description,
    DateTimeOffset CreatedAt);
