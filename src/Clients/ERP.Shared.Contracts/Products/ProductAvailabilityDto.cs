namespace ERP.Shared.Contracts.Products;

/// <summary>
/// Məhsulun bir anbardakı mövcudluğu (#18/#19): mövcud (total), rezervdə (təsdiqlənmiş),
/// kirayədə (təhvil verilmiş), boş (mövcud − rezerv − kirayə).
/// </summary>
public sealed record ProductAvailabilityDto(
    Guid WarehouseId,
    string WarehouseName,
    int Total,
    int Reserved,
    int Rented,
    int Free,
    int InRepair = 0,
    int Damaged = 0);

/// <summary>Məhsulun bütün anbarlar üzrə YEKUN stok xülasəsi (#27).</summary>
public sealed record StockSummaryDto(
    int Total,
    int Reserved,
    int Rented,
    int InRepair,
    int Damaged,
    int Free);
