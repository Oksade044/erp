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
    public string? Notes { get; private set; }

    public IReadOnlyList<OrderLine> Lines => _lines.AsReadOnly();

    /// <summary>Sifarişin ümumi məbləği (bütün sətirlərin cəmi).</summary>
    public Money Total => _lines.Aggregate(
        Money.Zero(), (sum, line) => sum.Add(line.LineTotal));

    /// <summary>Bu sifariş anbarı rezerv edirmi? (yalnız təsdiqlənmiş/təhvil verilmiş).</summary>
    public bool ReservesStock => Status is OrderStatus.Təsdiqlənmiş or OrderStatus.TəhvilVerilmiş;

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
        string? notes = null)
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
            Notes = notes?.Trim()
        };
    }

    public OrderLine AddLine(Guid productId, string productName, int quantity, Money unitPrice)
    {
        EnsureDraft();

        var existing = _lines.FirstOrDefault(l => l.ProductId == productId);
        if (existing is not null)
            throw new DomainException($"Bu məhsul artıq sifarişdə var: {productName}. Sayı dəyişin.");

        var line = new OrderLine(productId, productName, quantity, unitPrice);
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
        if (Status != OrderStatus.Qaralama)
            throw new DomainException("Yalnız qaralama sifariş təsdiqlənə bilər.");
        if (_lines.Count == 0)
            throw new DomainException("Boş sifariş təsdiqlənə bilməz.");
        Status = OrderStatus.Təsdiqlənmiş;
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

    private void EnsureDraft()
    {
        if (Status != OrderStatus.Qaralama)
            throw new DomainException("Sətirlər yalnız qaralama sifarişdə dəyişdirilə bilər.");
    }
}
