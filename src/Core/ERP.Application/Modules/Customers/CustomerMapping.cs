using ERP.Domain.Exceptions;
using ERP.Domain.Modules.Customers;
using ERP.Shared.Contracts.Customers;

namespace ERP.Application.Modules.Customers;

/// <summary>
/// Customer entity ↔ DTO çevirmələri (TDD §12). Value object-lər sadə tiplərə açılır.
/// Hələlik əl ilə mapping; Mapster gələcəkdə cross-cutting olaraq əlavə oluna bilər.
/// </summary>
public static class CustomerMapping
{
    public static CustomerDto ToDto(this Customer c) => new(
        Id: c.Id,
        Type: c.Type.ToString(),
        Name: c.Name,
        Phone: c.Phone.Value,
        Email: c.Email?.Value,
        City: c.Address?.City,
        AddressLine: c.Address?.Line,
        TaxId: c.TaxId,
        Notes: c.Notes,
        IsActive: c.IsActive,
        CreatedAt: c.CreatedAt,
        WhatsApp: c.WhatsApp,
        RepresentativeName: c.RepresentativeName,
        Debt: c.Debt?.Amount ?? 0m,
        DebtCurrency: c.Debt?.Currency ?? "AZN");

    public static CustomerType ParseType(string? type)
    {
        if (Enum.TryParse<CustomerType>(type, ignoreCase: true, out var parsed))
            return parsed;
        throw new DomainException($"Müştəri növü düzgün deyil: {type}. (Fərdi | Korporativ)");
    }

    public static Address? ToAddress(string? city, string? line) =>
        string.IsNullOrWhiteSpace(city) ? null : Address.Create(city, line);
}
