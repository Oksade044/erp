namespace ERP.Shared.Contracts.Customers;

/// <summary>Yeni müştəri yaratmaq üçün request DTO-su. Type: "Fərdi" | "Korporativ".</summary>
public sealed record CreateCustomerRequest(
    string Type,
    string Name,
    string Phone,
    string? Email = null,
    string? City = null,
    string? AddressLine = null,
    string? TaxId = null,
    string? Notes = null,
    // #1 — əlaqələndirmə + maliyyə
    string? WhatsApp = null,
    string? RepresentativeName = null,
    decimal Debt = 0,
    string DebtCurrency = "AZN");
