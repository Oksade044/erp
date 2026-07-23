using ERP.Domain.Modules.Customers;
using ERP.Domain.Modules.Suppliers;
using ERP.Shared.Contracts.Suppliers;

namespace ERP.Application.Modules.Suppliers;

/// <summary>Supplier entity ↔ DTO çevirmələri (TDD §12).</summary>
public static class SupplierMapping
{
    public static SupplierDto ToDto(this Supplier s) => new(
        Id: s.Id,
        Name: s.Name,
        ContactPerson: s.ContactPerson,
        Phone: s.Phone.Value,
        Email: s.Email?.Value,
        City: s.Address?.City,
        AddressLine: s.Address?.Line,
        TaxId: s.TaxId,
        Notes: s.Notes,
        IsActive: s.IsActive,
        CreatedAt: s.CreatedAt);

    public static Address? ToAddress(string? city, string? line) =>
        string.IsNullOrWhiteSpace(city) ? null : Address.Create(city, line);
}
