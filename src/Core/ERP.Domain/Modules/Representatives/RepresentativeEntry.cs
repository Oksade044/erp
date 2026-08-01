using ERP.Domain.Common;
using ERP.Domain.Exceptions;
using ERP.Domain.ValueObjects;

namespace ERP.Domain.Modules.Representatives;

/// <summary>Təmsilçi defter qeydinin növü (#16-18).</summary>
public enum RepEntryType
{
    /// <summary>Admin təmsilçiyə borc təyin edir (razılaşdırılmış məbləğ) — balansı azaldır.</summary>
    Borc = 1,
    /// <summary>Təmsilçi sifariş yaradır — balansı artırır (borcu bağlayır).</summary>
    Sifariş = 2,
    /// <summary>Sifariş ləğv edildi — əvvəlki krediti geri alır.</summary>
    SifarişLəğvi = 3,
    /// <summary>Təmsilçi nağd ödəyir — balansı artırır.</summary>
    Ödəniş = 4
}

/// <summary>
/// Təmsilçi (işçi) defter qeydi — aggregate root (#16-18). Təmsilçinin cari borcu bu qeydlərin
/// cəmidir. Admin razılaşdırılmış məbləği borc kimi yazır (mənfi); təmsilçi sifariş yaratdıqda
/// məbləğ kredit kimi əlavə olunur (borcu sıfırlayır/aşır). Təmsilçi User.FullName ilə əlaqələnir.
/// </summary>
public class RepresentativeEntry : BaseEntity, IAggregateRoot
{
    public string RepresentativeName { get; private set; } = null!;
    public DateOnly Date { get; private set; }
    public RepEntryType Type { get; private set; }
    public Money Amount { get; private set; } = null!;
    public string? Description { get; private set; }

    /// <summary>Sifariş qeydləri üçün sifariş nömrəsi (izləmə).</summary>
    public string? OrderNumber { get; private set; }

    /// <summary>Balansa təsir: Sifariş/Ödəniş → +, Borc/SifarişLəğvi → −.</summary>
    public decimal SignedAmount =>
        Type is RepEntryType.Sifariş or RepEntryType.Ödəniş ? Amount.Amount : -Amount.Amount;

    private RepresentativeEntry() { }

    private RepresentativeEntry(string representativeName, DateOnly date, RepEntryType type, Money amount)
    {
        RepresentativeName = representativeName;
        Date = date;
        Type = type;
        Amount = amount;
    }

    public static RepresentativeEntry Create(
        string representativeName, DateOnly date, RepEntryType type, Money amount,
        string? description = null, string? orderNumber = null)
    {
        if (string.IsNullOrWhiteSpace(representativeName))
            throw new DomainException("Təmsilçi tələb olunur.");
        if (amount.Amount <= 0)
            throw new DomainException("Məbləğ 0-dan böyük olmalıdır.");

        return new RepresentativeEntry(representativeName.Trim(), date, type, amount)
        {
            Description = description?.Trim(),
            OrderNumber = orderNumber
        };
    }
}
