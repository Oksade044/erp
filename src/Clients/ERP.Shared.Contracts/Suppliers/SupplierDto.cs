namespace ERP.Shared.Contracts.Suppliers;

/// <summary>Təchizatçı cavab DTO-su (TDD §12).</summary>
public sealed record SupplierDto(
    Guid Id,
    string Name,
    string? ContactPerson,
    string Phone,
    string? Email,
    string? City,
    string? AddressLine,
    string? TaxId,
    string? Notes,
    bool IsActive,
    DateTimeOffset CreatedAt);
