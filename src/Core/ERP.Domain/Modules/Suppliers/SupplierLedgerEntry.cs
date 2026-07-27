using ERP.Domain.Common;
using ERP.Domain.Exceptions;
using ERP.Domain.ValueObjects;

namespace ERP.Domain.Modules.Suppliers;

/// <summary>
/// Təchizatçı defter/tarixçə qeydi (#15) — aggregate root. Bir təchizatçıya aid vahid zaman xətti:
/// borc/ödəniş (maliyyə), danışıq (kommunikasiya) və sənəd (fayl) qeydləri. Borc − Ödəniş = qalıq borc.
/// Borc/Ödəniş üçün məbləğ tələb olunur; Danışıq/Sənəd üçün məbləğ sıfırdır.
/// </summary>
public class SupplierLedgerEntry : BaseEntity, IAggregateRoot
{
    public Guid SupplierId { get; private set; }
    public DateOnly Date { get; private set; }
    public SupplierEntryType Type { get; private set; }

    /// <summary>Borc/Ödəniş məbləği (Danışıq/Sənəd üçün 0).</summary>
    public Money Amount { get; private set; } = null!;

    /// <summary>Qeyd/açıqlama (danışıq mətni, ödəniş qeydi, sənəd adı və s.).</summary>
    public string? Description { get; private set; }

    /// <summary>Sənəd faylının açarı (yalnız Sənəd növü üçün; IFileStorage-də saxlanılır).</summary>
    public string? DocumentPath { get; private set; }

    /// <summary>Qalıq borca təsir: +borc, −ödəniş, digərləri 0.</summary>
    public decimal SignedAmount => Type switch
    {
        SupplierEntryType.Borc => Amount.Amount,
        SupplierEntryType.Ödəniş => -Amount.Amount,
        _ => 0m
    };

    // EF Core üçün.
    private SupplierLedgerEntry() { }

    private SupplierLedgerEntry(Guid supplierId, DateOnly date, SupplierEntryType type, Money amount)
    {
        SupplierId = supplierId;
        Date = date;
        Type = type;
        Amount = amount;
    }

    public static SupplierLedgerEntry Create(
        Guid supplierId,
        DateOnly date,
        SupplierEntryType type,
        Money amount,
        string? description = null,
        string? documentPath = null)
    {
        if (supplierId == Guid.Empty)
            throw new DomainException("Təchizatçı tələb olunur.");

        var isMoney = type is SupplierEntryType.Borc or SupplierEntryType.Ödəniş;
        if (isMoney && amount.Amount <= 0)
            throw new DomainException("Borc/ödəniş məbləği 0-dan böyük olmalıdır.");
        if (!isMoney)
            amount = Money.Zero(amount.Currency);

        if (type == SupplierEntryType.Danışıq && string.IsNullOrWhiteSpace(description))
            throw new DomainException("Danışıq qeydi üçün mətn tələb olunur.");

        return new SupplierLedgerEntry(supplierId, date, type, amount)
        {
            Description = description?.Trim(),
            DocumentPath = documentPath
        };
    }

    public void AttachDocument(string documentPath) => DocumentPath = documentPath;
}
