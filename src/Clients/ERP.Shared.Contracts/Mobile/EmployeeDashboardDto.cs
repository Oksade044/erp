namespace ERP.Shared.Contracts.Mobile;

/// <summary>
/// İşçiyə xas dashboard (mobil tətbiq) — yalnız cari işçinin sifarişləri üzrə statistika.
/// "Kim yaradıb" (CreatedByName) snapshot-ı üzrə hesablanır.
/// </summary>
public sealed record EmployeeDashboardDto(
    string EmployeeName,
    int DeliveriesToday,      // bu gün təhvil veriləcək (StartDate = bu gün, təsdiqlənmiş)
    int ReturnsToday,         // bu gün qaytarılacaq (EndDate = bu gün, təhvil verilmiş)
    int ActiveOrders,         // hazırda kirayədə (təhvil verilmiş)
    int PendingOrders,        // gözləyən (qaralama + təsdiqlənmiş)
    int OrdersThisMonth,      // bu ay yaratdığı sifariş sayı
    decimal RevenueThisMonth, // bu ay dövriyyəsi
    string Currency);

/// <summary>İşçinin maliyyə statistikası — dövr üzrə (mobil "Maliyyəm").</summary>
public sealed record EmployeeFinanceDto(
    string EmployeeName,
    decimal RevenueToday,
    decimal RevenueThisWeek,
    decimal RevenueThisMonth,
    decimal RevenueThisYear,
    int OrdersThisMonth,
    decimal RentalRevenueThisMonth,
    decimal SaleRevenueThisMonth,
    string Currency);
