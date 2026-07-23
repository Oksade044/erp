using ERP.Domain.Common;
using ERP.Domain.Exceptions;
using ERP.Domain.Modules.Customers;

namespace ERP.Domain.Modules.Suppliers;

/// <summary>
/// Təchizatçı — aggregate root, rich domain model (TDD §13). Avadanlıq/məhsul təchiz edən
/// tərəfdaş. Biznes qaydaları öz içindədir: ad və telefon məcburidir, əlaqədar şəxs opsional.
/// Value object-lər (PhoneNumber/Email/Address) Customers modulundan təkrar-istifadə olunur (DRY).
/// </summary>
public class Supplier : BaseEntity, IAggregateRoot
{
    public string Name { get; private set; } = null!;

    /// <summary>Əlaqədar şəxs (opsional).</summary>
    public string? ContactPerson { get; private set; }

    public PhoneNumber Phone { get; private set; } = null!;
    public Email? Email { get; private set; }
    public Address? Address { get; private set; }

    /// <summary>VÖEN (opsional).</summary>
    public string? TaxId { get; private set; }

    public string? Notes { get; private set; }
    public bool IsActive { get; private set; } = true;

    // EF Core üçün.
    private Supplier() { }

    private Supplier(string name, PhoneNumber phone)
    {
        Name = name;
        Phone = phone;
    }

    public static Supplier Create(
        string name,
        PhoneNumber phone,
        string? contactPerson = null,
        Email? email = null,
        Address? address = null,
        string? taxId = null,
        string? notes = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Təchizatçı adı tələb olunur.");

        return new Supplier(name.Trim(), phone)
        {
            ContactPerson = string.IsNullOrWhiteSpace(contactPerson) ? null : contactPerson.Trim(),
            Email = email,
            Address = address,
            TaxId = string.IsNullOrWhiteSpace(taxId) ? null : taxId.Trim(),
            Notes = notes?.Trim()
        };
    }

    public void Rename(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Təchizatçı adı tələb olunur.");
        Name = name.Trim();
    }

    public void UpdateContact(PhoneNumber phone, Email? email, string? contactPerson)
    {
        Phone = phone ?? throw new DomainException("Telefon nömrəsi tələb olunur.");
        Email = email;
        ContactPerson = string.IsNullOrWhiteSpace(contactPerson) ? null : contactPerson.Trim();
    }

    public void ChangeAddress(Address? address) => Address = address;
    public void SetTaxId(string? taxId) => TaxId = string.IsNullOrWhiteSpace(taxId) ? null : taxId.Trim();
    public void SetNotes(string? notes) => Notes = notes?.Trim();

    public void Deactivate() => IsActive = false;
    public void Activate() => IsActive = true;
}
