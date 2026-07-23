namespace ERP.Shared.Contracts.Purchases;

/// <summary>Alış sifarişi cavab DTO-su (TDD §12).</summary>
public sealed record PurchaseDto(
    Guid Id,
    string PurchaseNumber,
    Guid SupplierId,
    string SupplierName,
    DateOnly OrderDate,
    string Status,
    decimal Total,
    string Currency,
    string? Notes,
    DateTimeOffset CreatedAt,
    IReadOnlyList<PurchaseLineDto> Lines);

public sealed record PurchaseLineDto(
    Guid ProductId,
    string ProductName,
    int Quantity,
    decimal UnitCost,
    decimal LineTotal);
