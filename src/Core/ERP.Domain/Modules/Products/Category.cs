using ERP.Domain.Common;
using ERP.Domain.Exceptions;

namespace ERP.Domain.Modules.Products;

/// <summary>
/// Məhsul kateqoriyası — aggregate root (TDD §13). Sadə adlandırılmış lüğət;
/// məhsul formasında mövcud kateqoriyalardan seçim üçün. Ad unikaldır (böyük/kiçik hərfə həssas deyil).
/// </summary>
public class Category : BaseEntity, IAggregateRoot
{
    public string Name { get; private set; } = null!;
    public bool IsActive { get; private set; } = true;

    // EF Core üçün.
    private Category() { }

    public static Category Create(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Kateqoriya adı tələb olunur.");

        return new Category { Name = name.Trim() };
    }

    public void Rename(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Kateqoriya adı tələb olunur.");
        Name = name.Trim();
    }

    public void Activate() => IsActive = true;
    public void Deactivate() => IsActive = false;
}
