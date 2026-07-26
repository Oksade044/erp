using ERP.Domain.Common;
using ERP.Domain.Exceptions;
using ERP.Domain.ValueObjects;

namespace ERP.Domain.Modules.Orders;

/// <summary>
/// İcarə sifarişi — aggregate root (TDD §13). Müştəri + məhsul sətirlərini tarix aralığında
/// birləşdirir. Sətirlər yalnız bu aggregate vasitəsilə dəyişdirilir (bütövlük sərhədi).
/// Status keçidləri və anbar rezervi biznes invariantları ilə qorunur.
/// </summary>
public class RentalOrder : BaseEntity, IAggregateRoot
{
    private readonly List<OrderLine> _lines = [];

    public string OrderNumber { get; private set; } = null!;
    public Guid CustomerId { get; private set; }

    /// <summary>Müştəri adı — snapshot (sifariş tarixçəsi üçün).</summary>
    public string CustomerName { get; private set; } = null!;

    public DateOnly StartDate { get; private set; }
    public DateOnly EndDate { get; private set; }
    public OrderStatus Status { get; private set; } = OrderStatus.Qaralama;

    /// <summary>Sifariş növü — İcarə (default) və ya Satış (#33).</summary>
    public OrderType OrderType { get; private set; } = OrderType.İcarə;
    public string? Notes { get; private set; }

    // --- İcarə depozit & qaytarma hesablaşması (nullable → mövcud sətirlər üçün migration-safe) ---

    /// <summary>Girov/depozit məbləği (təhvildə saxlanılır, qaytarmada geri qaytarılır).</summary>
    public Money? Deposit { get; private set; }

    /// <summary>Qaytarmada qeyd olunan zədə/itki dəyəri.</summary>
    public Money? DamageCharge { get; private set; }

    /// <summary>Qaytarmada qeyd olunan cərimə (gecikmə və s.).</summary>
    public Money? PenaltyCharge { get; private set; }

    public string? SettlementNotes { get; private set; }
    public bool IsSettled { get; private set; }

    // --- Sifarişi yaradan (login-dən avtomatik snapshot — TDD §20 audit) ---

    /// <summary>Sifarişi yaradan istifadəçinin tam adı (yaradılan anda snapshot).</summary>
    public string? CreatedByName { get; private set; }

    /// <summary>Sifarişi yaradan istifadəçinin rolu (Admin/Menecer/...).</summary>
    public string? CreatedByRole { get; private set; }

    /// <summary>Sifarişi təsdiqləyən istifadəçinin adı (təsdiq anında snapshot).</summary>
    public string? ConfirmedByName { get; private set; }

    public IReadOnlyList<OrderLine> Lines => _lines.AsReadOnly();

    /// <summary>Sifarişin ümumi məbləği (bütün sətirlərin cəmi).</summary>
    public Money Total => _lines.Aggregate(
        Money.Zero(), (sum, line) => sum.Add(line.LineTotal));

    /// <summary>Bu sifariş anbarı rezerv edirmi? (yalnız təsdiqlənmiş/təhvil verilmiş).</summary>
    public bool ReservesStock => Status is OrderStatus.Təsdiqlənmiş or OrderStatus.TəhvilVerilmiş;

    /// <summary>Ümumi tutulma (zədə + cərimə).</summary>
    public Money TotalCharges =>
        Money.Create((DamageCharge?.Amount ?? 0m) + (PenaltyCharge?.Amount ?? 0m));

    /// <summary>Geri qaytarılacaq depozit = depozit − tutulmalar (0-dan aşağı düşmür).</summary>
    public Money DepositRefund
    {
        get
        {
            var refund = (Deposit?.Amount ?? 0m) - TotalCharges.Amount;
            return Money.Create(refund < 0m ? 0m : refund);
        }
    }

    // EF Core üçün.
    private RentalOrder() { }

    private RentalOrder(string orderNumber, Guid customerId, string customerName,
        DateOnly startDate, DateOnly endDate)
    {
        OrderNumber = orderNumber;
        CustomerId = customerId;
        CustomerName = customerName;
        StartDate = startDate;
        EndDate = endDate;
    }

    public static RentalOrder Create(
        string orderNumber,
        Guid customerId,
        string customerName,
        DateOnly startDate,
        DateOnly endDate,
        string? notes = null,
        string? createdByName = null,
        string? createdByRole = null,
        OrderType orderType = OrderType.İcarə)
    {
        if (string.IsNullOrWhiteSpace(orderNumber))
            throw new DomainException("Sifariş nömrəsi tələb olunur.");
        if (customerId == Guid.Empty)
            throw new DomainException("Sifariş üçün müştəri tələb olunur.");
        if (string.IsNullOrWhiteSpace(customerName))
            throw new DomainException("Müştəri adı tələb olunur.");
        if (endDate < startDate)
            throw new DomainException("Bitmə tarixi başlama tarixindən əvvəl ola bilməz.");

        return new RentalOrder(orderNumber, customerId, customerName.Trim(), startDate, endDate)
        {
            OrderType = orderType,
            Notes = notes?.Trim(),
            CreatedByName = string.IsNullOrWhiteSpace(createdByName) ? null : createdByName.Trim(),
            CreatedByRole = string.IsNullOrWhiteSpace(createdByRole) ? null : createdByRole.Trim()
        };
    }

    public OrderLine AddLine(Guid productId, string productName, int quantity, Money unitPrice,
        Guid? warehouseId = null, string? warehouseName = null)
    {
        EnsureDraft();

        var existing = _lines.FirstOrDefault(l => l.ProductId == productId);
        if (existing is not null)
            throw new DomainException($"Bu məhsul artıq sifarişdə var: {productName}. Sayı dəyişin.");

        var line = new OrderLine(productId, productName, quantity, unitPrice, warehouseId, warehouseName);
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

    public void Confirm(string? confirmedByName = null)
    {
        if (Status != OrderStatus.Qaralama)
            throw new DomainException("Yalnız qaralama sifariş təsdiqlənə bilər.");
        if (_lines.Count == 0)
            throw new DomainException("Boş sifariş təsdiqlənə bilməz.");
        Status = OrderStatus.Təsdiqlənmiş;
        ConfirmedByName = string.IsNullOrWhiteSpace(confirmedByName) ? null : confirmedByName.Trim();
    }

    public void Deliver()
    {
        if (Status != OrderStatus.Təsdiqlənmiş)
            throw new DomainException("Yalnız təsdiqlənmiş sifariş təhvil verilə bilər.");
        Status = OrderStatus.TəhvilVerilmiş;
    }

    public void Return()
    {
        if (Status != OrderStatus.TəhvilVerilmiş)
            throw new DomainException("Yalnız təhvil verilmiş sifariş qaytarıla bilər.");
        Status = OrderStatus.Qaytarılmış;
    }

    public void Cancel()
    {
        if (Status is OrderStatus.Qaytarılmış or OrderStatus.Ləğv)
            throw new DomainException("Bu sifariş ləğv edilə bilməz.");
        Status = OrderStatus.Ləğv;
    }

    /// <summary>Depozit məbləğini təyin edir. Qaytarılmış/ləğv sifarişdə dəyişdirilə bilməz.</summary>
    public void SetDeposit(Money deposit)
    {
        if (Status is OrderStatus.Qaytarılmış or OrderStatus.Ləğv)
            throw new DomainException("Qaytarılmış və ya ləğv edilmiş sifarişdə depozit dəyişdirilə bilməz.");
        Deposit = deposit ?? throw new DomainException("Depozit məbləği tələb olunur.");
    }

    /// <summary>
    /// Qaytarma hesablaşması: zədə/itki dəyəri və cəriməni qeyd edir. Yalnız təhvil verilmiş
    /// və ya qaytarılmış sifarişdə mümkündür. DepositRefund avtomatik hesablanır.
    /// </summary>
    public void Settle(Money damageCharge, Money penaltyCharge, string? notes = null)
    {
        if (Status is not (OrderStatus.TəhvilVerilmiş or OrderStatus.Qaytarılmış))
            throw new DomainException("Hesablaşma yalnız təhvil verilmiş və ya qaytarılmış sifarişdə mümkündür.");

        DamageCharge = damageCharge ?? throw new DomainException("Zədə dəyəri tələb olunur.");
        PenaltyCharge = penaltyCharge ?? throw new DomainException("Cərimə dəyəri tələb olunur.");
        SettlementNotes = notes?.Trim();
        IsSettled = true;
    }

    private void EnsureDraft()
    {
        if (Status != OrderStatus.Qaralama)
            throw new DomainException("Sətirlər yalnız qaralama sifarişdə dəyişdirilə bilər.");
    }
}
