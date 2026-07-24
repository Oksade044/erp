namespace ERP.Shared.Contracts.Warehouses;

/// <summary>Anbar cavab DTO-su (TDD §12).</summary>
public sealed record WarehouseDto(
    Guid Id,
    string Name,
    string Code,
    string? Phone,
    string? City,
    string? AddressLine,
    string? Notes,
    bool IsActive,
    DateTimeOffset CreatedAt);
