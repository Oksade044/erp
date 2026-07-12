using System.Text.RegularExpressions;
using ERP.Domain.Common;
using ERP.Domain.Exceptions;

namespace ERP.Domain.Modules.Customers;

/// <summary>
/// Telefon nömrəsi value object-i. Azərbaycan formatı normalizə olunur: +994XXXXXXXXX.
/// Yanlış nömrə qeyri-mümkündür (TDD §13).
/// </summary>
public sealed partial class PhoneNumber : ValueObject
{
    public string Value { get; }

    private PhoneNumber(string value) => Value = value;

    public static PhoneNumber Create(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            throw new DomainException("Telefon nömrəsi boş ola bilməz.");

        // Yalnız rəqəmləri saxla.
        var digits = DigitsOnly().Replace(raw, "");

        // Azərbaycan nömrələrini +994 formatına gətir.
        var normalized = digits switch
        {
            { Length: 12 } when digits.StartsWith("994") => "+" + digits,
            { Length: 10 } when digits.StartsWith("0") => "+994" + digits[1..],
            { Length: 9 } => "+994" + digits,
            _ => throw new DomainException($"Telefon nömrəsi düzgün deyil: {raw}")
        };

        return new PhoneNumber(normalized);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;

    [GeneratedRegex(@"\D")]
    private static partial Regex DigitsOnly();
}
