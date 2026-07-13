using ERP.Domain.Common;
using ERP.Domain.Exceptions;
using ERP.Domain.ValueObjects;

namespace ERP.Domain.Modules.Invoices;

/// <summary>
/// Faktura — aggregate root (TDD §13). Bir icarə sifarişindən yaradılır; sifarişin
/// ümumi məbləği və müştəri məlumatı snapshot kimi saxlanılır. Ödənişlər burada idarə olunur;
/// status ödənilmiş məbləğə görə hesablanır.
/// </summary>
public class Invoice : BaseEntity, IAggregateRoot
{
    private readonly List<Payment> _payments = [];

    public string InvoiceNumber { get; private set; } = null!;
    public Guid OrderId { get; private set; }
    public string OrderNumber { get; private set; } = null!;
    public Guid CustomerId { get; private set; }
    public string CustomerName { get; private set; } = null!;
    public DateOnly IssueDate { get; private set; }

    /// <summary>Faktura məbləği — sifariş cəminin snapshot-ı.</summary>
    public Money TotalAmount { get; private set; } = null!;

    public IReadOnlyList<Payment> Payments => _payments.AsReadOnly();

    /// <summary>Ödənilmiş ümumi məbləğ.</summary>
    public Money AmountPaid => _payments.Aggregate(
        Money.Zero(TotalAmount.Currency), (sum, p) => sum.Add(p.Amount));

    /// <summary>Qalıq borc.</summary>
    public Money Balance => Money.Create(TotalAmount.Amount - AmountPaid.Amount, TotalAmount.Currency);

    public InvoiceStatus Status =>
        AmountPaid.Amount <= 0 ? InvoiceStatus.Ödənilməmiş
        : AmountPaid.Amount >= TotalAmount.Amount ? InvoiceStatus.Ödənilmiş
        : InvoiceStatus.QismənÖdənilmiş;

    // EF Core üçün.
    private Invoice() { }

    private Invoice(string invoiceNumber, Guid orderId, string orderNumber,
        Guid customerId, string customerName, DateOnly issueDate, Money totalAmount)
    {
        InvoiceNumber = invoiceNumber;
        OrderId = orderId;
        OrderNumber = orderNumber;
        CustomerId = customerId;
        CustomerName = customerName;
        IssueDate = issueDate;
        TotalAmount = totalAmount;
    }

    public static Invoice Create(
        string invoiceNumber,
        Guid orderId,
        string orderNumber,
        Guid customerId,
        string customerName,
        DateOnly issueDate,
        Money totalAmount)
    {
        if (string.IsNullOrWhiteSpace(invoiceNumber))
            throw new DomainException("Faktura nömrəsi tələb olunur.");
        if (orderId == Guid.Empty)
            throw new DomainException("Faktura üçün sifariş tələb olunur.");
        if (totalAmount.Amount <= 0)
            throw new DomainException("Faktura məbləği 0-dan böyük olmalıdır.");

        return new Invoice(invoiceNumber, orderId, orderNumber, customerId,
            customerName.Trim(), issueDate, totalAmount);
    }

    public Payment AddPayment(Money amount, DateOnly paidAt, PaymentMethod method, string? note = null)
    {
        if (amount.Currency != TotalAmount.Currency)
            throw new DomainException("Ödəniş valyutası faktura valyutası ilə eyni olmalıdır.");
        if (amount.Amount > Balance.Amount)
            throw new DomainException(
                $"Ödəniş qalıq borcdan çox ola bilməz. Qalıq: {Balance}, ödəniş: {amount}.");

        var payment = new Payment(amount, paidAt, method, note);
        _payments.Add(payment);
        return payment;
    }
}
