namespace ERP.Shared.Contracts.Orders;

/// <summary>
/// Qaytarma hesablaşması üçün request DTO-su — zədə/itki dəyəri və cərimə.
/// DepositRefund server tərəfdə hesablanır.
/// </summary>
public sealed record SettleOrderRequest(
    decimal DamageCharge = 0,
    decimal PenaltyCharge = 0,
    string? Notes = null);
