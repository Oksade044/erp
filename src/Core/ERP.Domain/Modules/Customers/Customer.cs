using ERP.Domain.Common;
using ERP.Domain.Exceptions;
using ERP.Domain.ValueObjects;

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

    /// <summary>WhatsApp nömrəsi (#1).</summary>
    public string? WhatsApp { get; private set; }

    /// <summary>Müştərinin aid olduğu təmsilçi/nümayəndə (#1 Əlaqələndirmə). Sonradan dəyişdirilə bilər.</summary>
    public string? RepresentativeName { get; private set; }

    /// <summary>Müştərinin bizə olan borcu (#1 Maliyyə). Valyuta Money-də saxlanılır.</summary>
    public Money? Debt { get; private set; }

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
        string? notes = null,
        string? whatsApp = null,
        string? representativeName = null,
        Money? debt = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Müştəri adı tələb olunur.");

        var customer = new Customer(type, name.Trim(), phone)
        {
            Email = email,
            Address = address,
            TaxId = NormalizeTaxId(type, taxId),
            Notes = notes?.Trim(),
            WhatsApp = string.IsNullOrWhiteSpace(whatsApp) ? null : whatsApp.Trim(),
            RepresentativeName = string.IsNullOrWhiteSpace(representativeName) ? null : representativeName.Trim(),
            Debt = debt
        };

        return customer;
    }

    /// <summary>Biznes sahələrini yeniləyir (#1): WhatsApp, təmsilçi, borc.</summary>
    public void UpdateBusiness(string? whatsApp, string? representativeName, Money? debt)
    {
        WhatsApp = string.IsNullOrWhiteSpace(whatsApp) ? null : whatsApp.Trim();
        RepresentativeName = string.IsNullOrWhiteSpace(representativeName) ? null : representativeName.Trim();
        Debt = debt;
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
