namespace ERP.Shared.Contracts.Products;

/// <summary>
/// Məhsulun istifadə tarixçəsi sətri (#38) — hansı sifarişdə, kimə, neçəyə, nə vaxt.
/// Məhsul silinsə belə saxlanılır (sifariş sətri qeydinə əsaslanır).
/// </summary>
public sealed record ProductHistoryRowDto(
    string OrderNumber,
    string? InvoiceNumber,
    string CustomerName,
    string? EmployeeName,
    string OrderType,
    string Status,
    int Quantity,
    decimal UnitPrice,
    string Currency,
    DateOnly StartDate,
    DateOnly EndDate,
    int Days,
    string? WarehouseName);
