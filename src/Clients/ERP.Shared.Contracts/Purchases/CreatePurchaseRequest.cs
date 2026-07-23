namespace ERP.Shared.Contracts.Purchases;

/// <summary>Yeni alış sifarişi yaratmaq üçün request DTO-su (Qaralama statusunda yaranır).</summary>
public sealed record CreatePurchaseRequest(
    Guid SupplierId,
    DateOnly OrderDate,
    IReadOnlyList<CreatePurchaseLineRequest> Lines,
    string? Notes = null);

/// <summary>Alış sətri — məhsul + say + vahid alış qiyməti.</summary>
public sealed record CreatePurchaseLineRequest(
    Guid ProductId,
    int Quantity,
    decimal UnitCost);
