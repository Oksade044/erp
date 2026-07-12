namespace ERP.Shared.Contracts.Customers;

/// <summary>Mövcud müştərini yeniləmək üçün request DTO-su.</summary>
public sealed record UpdateCustomerRequest(
    string Name,
    string Phone,
    string? Email = null,
    string? City = null,
    string? AddressLine = null,
    string? Notes = null,
    bool IsActive = true);
