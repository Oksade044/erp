using ERP.Domain.Common;
using ERP.Domain.Exceptions;
using ERP.Domain.ValueObjects;

namespace ERP.Domain.Modules.Purchases;

/// <summary>
/// Alış sifarişi — aggregate root (TDD §13). Təchizatçıdan mal alışını sətirlərlə birləşdirir.
/// Sətirlər yalnız bu aggregate vasitəsilə dəyişdirilir (bütövlük sərhədi). Status keçidləri
/// biznes invariantları ilə qorunur; qəbul (QəbulEdilmiş) məhsul stokunu artıran addımdır.
/// </summary>
public class PurchaseOrder : BaseEntity, IAggregateRoot
{
    private readonly List<PurchaseLine> _lines = [];

    public string PurchaseNumber { get; private set; } = null!;
    public Guid SupplierId { get; private set; }

    /// <summary>Təchizatçı adı — snapshot (alış tarixçəsi üçün).</summary>
    public string SupplierName { get; private set; } = null!;

    public DateOnly OrderDate { get; private set; }
    public PurchaseStatus Status { get; private set; } = PurchaseStatus.Qaralama;
    public string? Notes { get; private set; }

    public IReadOnlyList<PurchaseLine> Lines => _lines.AsReadOnly();

    /// <summary>Alışın ümumi məbləği (bütün sətirlərin cəmi).</summary>
    public Money Total => _lines.Aggregate(Money.Zero(), (sum, line) => sum.Add(line.LineTotal));

    // EF Core üçün.
    private PurchaseOrder() { }

    private PurchaseOrder(string purchaseNumber, Guid supplierId, string supplierName, DateOnly orderDate)
    {
        PurchaseNumber = purchaseNumber;
        SupplierId = supplierId;
        SupplierName = supplierName;
        OrderDate = orderDate;
    }

    public static PurchaseOrder Create(
        string purchaseNumber,
        Guid supplierId,
        string supplierName,
        DateOnly orderDate,
        string? notes = null)
    {
        if (string.IsNullOrWhiteSpace(purchaseNumber))
            throw new DomainException("Alış nömrəsi tələb olunur.");
        if (supplierId == Guid.Empty)
            throw new DomainException("Alış üçün təchizatçı tələb olunur.");
        if (string.IsNullOrWhiteSpace(supplierName))
            throw new DomainException("Təchizatçı adı tələb olunur.");

        return new PurchaseOrder(purchaseNumber, supplierId, supplierName.Trim(), orderDate)
        {
            Notes = notes?.Trim()
        };
    }

    public PurchaseLine AddLine(Guid productId, string productName, int quantity, Money unitCost)
    {
        EnsureDraft();

        if (_lines.Any(l => l.ProductId == productId))
            throw new DomainException($"Bu məhsul artıq alışda var: {productName}. Sayı dəyişin.");

        var line = new PurchaseLine(productId, productName, quantity, unitCost);
        _lines.Add(line);
        return line;
    }

    public void ChangeLineQuantity(Guid productId, int quantity)
    {
        EnsureDraft();
        var line = _lines.FirstOrDefault(l => l.ProductId == productId)
            ?? throw new DomainException("Sətir tapılmadı.");
        line.ChangeQuantity(quantity);
    }

    public void RemoveLine(Guid productId)
    {
        EnsureDraft();
        _lines.RemoveAll(l => l.ProductId == productId);
    }

    public void SetNotes(string? notes) => Notes = notes?.Trim();

    // --- Status keçidləri ---

    public void Confirm()
    {
        if (Status != PurchaseStatus.Qaralama)
            throw new DomainException("Yalnız qaralama alış təsdiqlənə bilər.");
        if (_lines.Count == 0)
            throw new DomainException("Boş alış təsdiqlənə bilməz.");
        Status = PurchaseStatus.Təsdiqlənmiş;
    }

    /// <summary>
    /// Malın anbara qəbulu. Yalnız statusu dəyişir — məhsul stokunun artırılması handler
    /// səviyyəsində (Product aggregate üzərində) aparılır, çünki fərqli aggregate-dir.
    /// </summary>
    public void Receive()
    {
        if (Status != PurchaseStatus.Təsdiqlənmiş)
            throw new DomainException("Yalnız təsdiqlənmiş alış qəbul edilə bilər.");
        Status = PurchaseStatus.QəbulEdilmiş;
    }

    public void Cancel()
    {
        if (Status is PurchaseStatus.QəbulEdilmiş or PurchaseStatus.Ləğv)
            throw new DomainException("Bu alış ləğv edilə bilməz.");
        Status = PurchaseStatus.Ləğv;
    }

    private void EnsureDraft()
    {
        if (Status != PurchaseStatus.Qaralama)
            throw new DomainException("Sətirlər yalnız qaralama alışda dəyişdirilə bilər.");
    }
}
