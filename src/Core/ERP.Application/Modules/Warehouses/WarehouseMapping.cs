using ERP.Domain.Modules.Customers;
using ERP.Domain.Modules.Warehouses;
using ERP.Shared.Contracts.Warehouses;

namespace ERP.Application.Modules.Warehouses;

/// <summary>Warehouse entity ↔ DTO çevirmələri (TDD §12).</summary>
public static class WarehouseMapping
{
    public static WarehouseDto ToDto(this Warehouse w) => new(
        Id: w.Id,
        Name: w.Name,
        Code: w.Code,
        Phone: w.Phone?.Value,
        City: w.Address?.City,
        AddressLine: w.Address?.Line,
        Notes: w.Notes,
        IsActive: w.IsActive,
        CreatedAt: w.CreatedAt);

    public static Address? ToAddress(string? city, string? line) =>
        string.IsNullOrWhiteSpace(city) ? null : Address.Create(city, line);
}
