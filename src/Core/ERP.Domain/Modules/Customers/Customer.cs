using ERP.Domain.Common;
using ERP.Domain.Exceptions;

namespace ERP.Domain.Modules.Customers;

/// <summary>
/// Müştəri — aggregate root, rich domain model (TDD §13). Biznes qaydaları öz içindədir:
/// ad tələb olunur, telefon məcburidir, korporativ müştəri üçün VÖEN saxlana bilər.
/// Dəyişikliklər yalnız metodlar vasitəsilə olur — property-lər xaricdən dəyişdirilə bilməz.
/// </summary>
public class Customer : BaseEntity, IAggregateRoot
{
    public CustomerType Type { get; private set; }
    public string Name { get; private set; } = null!;
    public PhoneNumber Phone { get; private set; } = null!;
    public Email? Email { get; private set; }
    public Address? Address { get; private set; }

    /// <summary>Korporativ müştəri üçün VÖEN (opsional).</summary>
    public string? TaxId { get; private set; }

    public string? Notes { get; private set; }
    public bool IsActive { get; private set; } = true;

    // EF Core üçün.
    private Customer() { }

    private Customer(CustomerType type, string name, PhoneNumber phone)
    {
        Type = type;
        Name = name;
        Phone = phone;
    }

    public static Customer Create(
        CustomerType type,
        string name,
        PhoneNumber phone,
        Email? email = null,
        Address? address = null,
        string? taxId = null,
        string? notes = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Müştəri adı tələb olunur.");

        var customer = new Customer(type, name.Trim(), phone)
        {
            Email = email,
            Address = address,
            TaxId = NormalizeTaxId(type, taxId),
            Notes = notes?.Trim()
        };

        return customer;
    }

    public void UpdateContact(PhoneNumber phone, Email? email)
    {
        Phone = phone ?? throw new DomainException("Telefon nömrəsi tələb olunur.");
        Email = email;
    }

    public void ChangeAddress(Address? address) => Address = address;

    public void Rename(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Müştəri adı tələb olunur.");
        Name = name.Trim();
    }

    public void SetNotes(string? notes) => Notes = notes?.Trim();

    public void Deactivate() => IsActive = false;
    public void Activate() => IsActive = true;

    private static string? NormalizeTaxId(CustomerType type, string? taxId)
    {
        if (string.IsNullOrWhiteSpace(taxId))
            return null;

        // VÖEN yalnız korporativ müştəri üçün mənalıdır.
        if (type != CustomerType.Korporativ)
            throw new DomainException("VÖEN yalnız korporativ müştəri üçün təyin oluna bilər.");

        return taxId.Trim();
    }
}
