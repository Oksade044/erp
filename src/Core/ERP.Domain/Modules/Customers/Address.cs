using ERP.Domain.Common;
using ERP.Domain.Exceptions;

namespace ERP.Domain.Modules.Customers;

/// <summary>
/// Ünvan value object-i (şəhər + ünvan sətri). Opsional, amma verildikdə şəhər tələb olunur.
/// Çatdırılma/quraşdırma üçün istifadə olunur.
/// </summary>
public sealed class Address : ValueObject
{
    public string City { get; }
    public string Line { get; }

    private Address(string city, string line)
    {
        City = city;
        Line = line;
    }

    public static Address Create(string city, string? line = null)
    {
        if (string.IsNullOrWhiteSpace(city))
            throw new DomainException("Ünvan üçün şəhər tələb olunur.");

        return new Address(city.Trim(), (line ?? string.Empty).Trim());
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return City;
        yield return Line;
    }

    public override string ToString() =>
        string.IsNullOrEmpty(Line) ? City : $"{City}, {Line}";
}
