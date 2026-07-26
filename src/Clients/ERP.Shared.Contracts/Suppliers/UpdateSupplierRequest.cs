namespace ERP.Shared.Contracts.Suppliers;

/// <summary>Mövcud təchizatçını yeniləmək üçün request DTO-su.</summary>
public sealed record UpdateSupplierRequest(
    string Name,
    string Phone,
    string? ContactPerson = null,
    string? Email = null,
    string? City = null,
    string? AddressLine = null,
    string? TaxId = null,
    string? Notes = null,
    bool IsActive = true,
    string? CompanyName = null,
    string? Country = null,
    string? WhatsApp = null,
    string? WeChat = null,
    string? Position = null);
