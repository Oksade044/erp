namespace ERP.Shared.Contracts.Warehouses;

/// <summary>Mövcud anbarı yeniləmək üçün request DTO-su.</summary>
public sealed record UpdateWarehouseRequest(
    string Name,
    string Code,
    string? Phone = null,
    string? City = null,
    string? AddressLine = null,
    string? Notes = null,
    bool IsActive = true);
