using ERP.Domain.Common;
using ERP.Domain.Exceptions;
using ERP.Domain.Modules.Customers;

namespace ERP.Domain.Modules.Warehouses;

/// <summary>
/// Anbar — aggregate root, rich domain model (TDD §13). Çox-anbar (multi-warehouse) blokunun
/// təməli; stok səviyyələri və anbarlar arası transfer buna istinad edəcək.
/// Kod unikaldır (normalizə: böyük hərf, kəsilmiş). Address/Phone VO Customers-dən təkrar-istifadə.
/// </summary>
public class Warehouse : BaseEntity, IAggregateRoot
{
    public string Name { get; private set; } = null!;

    /// <summary>Qısa unikal kod (məs. "ANBAR-1", "MAGAZA").</summary>
    public string Code { get; private set; } = null!;

    public Address? Address { get; private set; }
    public PhoneNumber? Phone { get; private set; }
    public string? Notes { get; private set; }
    public bool IsActive { get; private set; } = true;

    // EF Core üçün.
    private Warehouse() { }

    private Warehouse(string name, string code)
    {
        Name = name;
        Code = code;
    }

    public static Warehouse Create(
        string name,
        string code,
        Address? address = null,
        PhoneNumber? phone = null,
        string? notes = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Anbar adı tələb olunur.");

        return new Warehouse(name.Trim(), NormalizeCode(code))
        {
            Address = address,
            Phone = phone,
            Notes = notes?.Trim()
        };
    }

    public void Rename(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Anbar adı tələb olunur.");
        Name = name.Trim();
    }

    public void ChangeCode(string code) => Code = NormalizeCode(code);
    public void ChangeAddress(Address? address) => Address = address;
    public void SetPhone(PhoneNumber? phone) => Phone = phone;
    public void SetNotes(string? notes) => Notes = notes?.Trim();

    public void Activate() => IsActive = true;
    public void Deactivate() => IsActive = false;

    private static string NormalizeCode(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new DomainException("Anbar kodu tələb olunur.");
        return code.Trim().ToUpperInvariant();
    }
}
