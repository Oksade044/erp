namespace ERP.Shared.Contracts.Finance;

/// <summary>
/// Yeni maliyyə əməliyyatı (mədaxil/məxaric) yaratmaq üçün request DTO-su.
/// Type: "Mədaxil" | "Məxaric". Method: "Nağd" | "Köçürmə" | "Kart".
/// </summary>
public sealed record CreateTransactionRequest(
    string Type,
    string Category,
    decimal Amount,
    DateOnly Date,
    string Method,
    string? Description = null);
