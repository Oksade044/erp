namespace ERP.Shared.Contracts.Orders;

/// <summary>Sifariş cavab DTO-su (sətirlərlə birlikdə). TDD §12.</summary>
public sealed record OrderDto(
    Guid Id,
    string OrderNumber,
    Guid CustomerId,
    string CustomerName,
    DateOnly StartDate,
    DateOnly EndDate,
    string Status,
    string OrderType,
    string? Notes,
    decimal Total,
    string Currency,
    decimal Deposit,
    decimal DamageCharge,
    decimal PenaltyCharge,
    decimal TotalCharges,
    decimal DepositRefund,
    string DepositStatus,
    bool IsSettled,
    string? SettlementNotes,
    string? CreatedByName,
    string? CreatedByRole,
    string? ConfirmedByName,
    IReadOnlyList<OrderLineDto> Lines,
    DateTimeOffset? BookedAt = null,
    DateTimeOffset? DeliveredAt = null,
    DateTimeOffset? ReturnedAt = null);

public sealed record OrderLineDto(
    Guid ProductId,
    string ProductName,
    int Quantity,
    decimal UnitPrice,
    decimal LineTotal,
    string Currency,
    string? WarehouseName = null);
