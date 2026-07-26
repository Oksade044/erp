namespace ERP.Shared.Contracts.Suppliers;

/// <summary>Yeni təchizatçı yaratmaq üçün request DTO-su.</summary>
public sealed record CreateSupplierRequest(
    string Name,
    string Phone,
    string? ContactPerson = null,
    string? Email = null,
    string? City = null,
    string? AddressLine = null,
    string? TaxId = null,
    string? Notes = null,
    string? CompanyName = null,
    string? Country = null,
    string? WhatsApp = null,
    string? WeChat = null,
    string? Position = null);
