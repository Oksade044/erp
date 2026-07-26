namespace ERP.Shared.Contracts.Orders;

/// <summary>
/// Yeni icarə sifarişi yaratmaq üçün request. Sifariş "Qaralama" statusunda yaradılır.
/// Sətirdə UnitPrice verilməzsə, məhsulun baza icarə qiyməti istifadə olunur (dinamik qiymət — TDD §7).
/// </summary>
public sealed record CreateOrderRequest(
    Guid CustomerId,
    DateOnly StartDate,
    DateOnly EndDate,
    IReadOnlyList<CreateOrderLineRequest> Lines,
    string? Notes = null,
    // Yalnız Admin/Menecer: sifarişi başqa məsul əməkdaşın adına yaza bilər.
    // Boşdursa, sifariş daxil olmuş istifadəçinin adına yazılır.
    string? CreatedByName = null,
    string? CreatedByRole = null);

public sealed record CreateOrderLineRequest(
    Guid ProductId,
    int Quantity,
    decimal? UnitPrice = null,
    // #18 — məhsulun götürüləcəyi anbar (opsional).
    Guid? WarehouseId = null);
