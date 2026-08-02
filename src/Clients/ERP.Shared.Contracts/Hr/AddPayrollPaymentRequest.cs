namespace ERP.Shared.Contracts.Hr;

/// <summary>Əməkhaqqıya hissə-hissə ödəniş / bonus əlavə etmək üçün request. Method: Nağd|Köçürmə|Kart.</summary>
public sealed record AddPayrollPaymentRequest(
    decimal Amount,
    DateOnly Date,
    string Method = "Nağd",
    string? Note = null);
