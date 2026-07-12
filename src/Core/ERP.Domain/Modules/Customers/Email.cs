using System.Text.RegularExpressions;
using ERP.Domain.Common;
using ERP.Domain.Exceptions;

namespace ERP.Domain.Modules.Customers;

/// <summary>
/// E-poçt value object-i. Opsional sahədir, amma verildikdə formatı yoxlanılır (TDD §13).
/// </summary>
public sealed partial class Email : ValueObject
{
    public string Value { get; }

    private Email(string value) => Value = value;

    public static Email Create(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            throw new DomainException("E-poçt boş ola bilməz.");

        var trimmed = raw.Trim().ToLowerInvariant();
        if (!EmailPattern().IsMatch(trimmed))
            throw new DomainException($"E-poçt düzgün deyil: {raw}");

        return new Email(trimmed);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;

    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$")]
    private static partial Regex EmailPattern();
}
