namespace ERP.Shared.Contracts.Reports;

/// <summary>İdarə paneli xülasəsi — əsas göstəricilər.</summary>
public sealed record DashboardDto(
    int CustomerCount,
    int ProductCount,
    int OrderCount,
    int DraftOrders,
    int ConfirmedOrders,
    int DeliveredOrders,
    int ReturnedOrders,
    int CancelledOrders,
    decimal TotalInvoiced,
    decimal TotalPaid,
    decimal TotalOutstanding,
    string Currency);

/// <summary>Qalıq borcu olan faktura.</summary>
public sealed record OutstandingInvoiceDto(
    string InvoiceNumber,
    string CustomerName,
    decimal Total,
    decimal Paid,
    decimal Balance,
    string Currency,
    string Status);

/// <summary>Ən çox icarəyə verilən məhsul.</summary>
public sealed record TopProductDto(
    string ProductName,
    int TotalQuantityRented,
    int OrderCount);
