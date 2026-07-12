using System.Text.RegularExpressions;
using ERP.Domain.Common;
using ERP.Domain.Exceptions;

namespace ERP.Domain.Modules.Products;

/// <summary>
/// SKU (Stock Keeping Unit) — məhsulun unikal anbar kodu. Normalizə olunur:
/// böyük hərf, yalnız hərf/rəqəm/tire. Yanlış kod qeyri-mümkündür (TDD §13).
/// </summary>
public sealed partial class Sku : ValueObject
{
    public string Value { get; }

    private Sku(string value) => Value = value;

    public static Sku Create(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            throw new DomainException("SKU boş ola bilməz.");

        var normalized = raw.Trim().ToUpperInvariant();
        if (!SkuPattern().IsMatch(normalized))
            throw new DomainException($"SKU yalnız hərf, rəqəm və tire ola bilər: {raw}");

        return new Sku(normalized);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;

    [GeneratedRegex(@"^[A-Z0-9\-]{2,40}$")]
    private static partial Regex SkuPattern();
}
