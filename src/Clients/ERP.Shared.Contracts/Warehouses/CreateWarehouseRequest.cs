namespace ERP.Shared.Contracts.Warehouses;

/// <summary>Yeni anbar yaratmaq üçün request DTO-su.</summary>
public sealed record CreateWarehouseRequest(
    string Name,
    string Code,
    string? Phone = null,
    string? City = null,
    string? AddressLine = null,
    string? Notes = null);
