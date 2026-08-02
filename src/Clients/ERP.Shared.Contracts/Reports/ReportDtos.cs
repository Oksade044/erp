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
    string Currency,
    // #20 — bugünkü əməliyyatlar
    int DeliveriesToday = 0,
    int ReturnsToday = 0,
    int OverdueReturns = 0,
    // #23 — maliyyə paneli (ödəniş tarixinə görə gəlir)
    decimal IncomeToday = 0,
    decimal IncomeThisWeek = 0,
    decimal IncomeThisMonth = 0,
    decimal IncomeThisYear = 0);

/// <summary>Qalıq borcu olan faktura.</summary>
public sealed record OutstandingInvoiceDto(
    string InvoiceNumber,
    string CustomerName,
    decimal Total,
    decimal Paid,
    decimal Balance,
    string Currency,
    string Status,
    Guid InvoiceId = default,
    string OrderNumber = "");

/// <summary>Ən çox icarəyə verilən məhsul.</summary>
public sealed record TopProductDto(
    string ProductName,
    int TotalQuantityRented,
    int OrderCount);
