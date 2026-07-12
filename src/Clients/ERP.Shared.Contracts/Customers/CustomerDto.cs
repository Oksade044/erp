namespace ERP.Shared.Contracts.Customers;

/// <summary>
/// Müştəri cavab DTO-su (TDD §12). Entity heç vaxt şəbəkəyə çıxmır — client və server
/// bu müqaviləni bölüşür. Domenə asılılıq yoxdur (Type/Address sadə tiplərlə ifadə olunur).
/// </summary>
public sealed record CustomerDto(
    Guid Id,
    string Type,
    string Name,
    string Phone,
    string? Email,
    string? City,
    string? AddressLine,
    string? TaxId,
    string? Notes,
    bool IsActive,
    DateTimeOffset CreatedAt);
