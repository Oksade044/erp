using ERP.Domain.Common;

namespace ERP.Domain.ValueObjects;

/// <summary>
/// Pul dəyəri (məbləğ + valyuta). Primitiv decimal əvəzinə istifadə olunur ki,
/// valyuta qarışması və mənfi məbləğ qeyri-mümkün olsun (TDD §13).
/// İcarə qiyməti burada modelləşir (məhsula/sifarişə görə dinamik — TDD prinsip §7).
/// </summary>
public sealed class Money : ValueObject
{
    public const string DefaultCurrency = "AZN";

    public decimal Amount { get; }
    public string Currency { get; }

    private Money(decimal amount, string currency)
    {
        Amount = amount;
        Currency = currency;
    }

    public static Money Create(decimal amount, string currency = DefaultCurrency)
    {
        if (amount < 0)
            throw new ArgumentException("Məbləğ mənfi ola bilməz.", nameof(amount));
        if (string.IsNullOrWhiteSpace(currency))
            throw new ArgumentException("Valyuta boş ola bilməz.", nameof(currency));

        return new Money(decimal.Round(amount, 2), currency.ToUpperInvariant());
    }

    public static Money Zero(string currency = DefaultCurrency) => new(0m, currency);

    public Money Add(Money other)
    {
        EnsureSameCurrency(other);
        return new Money(Amount + other.Amount, Currency);
    }

    public Money Multiply(decimal factor) => new(decimal.Round(Amount * factor, 2), Currency);

    private void EnsureSameCurrency(Money other)
    {
        if (Currency != other.Currency)
            throw new InvalidOperationException(
                $"Fərqli valyutalar toplana bilməz: {Currency} vs {other.Currency}.");
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Amount;
        yield return Currency;
    }

    public override string ToString() => $"{Amount:0.00} {Currency}";
}
