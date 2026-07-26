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
    DateTimeOffset CreatedAt,
    // V2 (#15)
    string? CompanyName = null,
    string? Country = null,
    string? WhatsApp = null,
    string? WeChat = null,
    string? Position = null);
