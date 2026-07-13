using ERP.Application.Common.Interfaces;
using ERP.Application.Common.Messaging;
using ERP.Application.Common.Models;
using ERP.Domain.ValueObjects;
using ERP.Shared.Contracts.Invoices;
using FluentValidation;

namespace ERP.Application.Modules.Invoices.Commands;

/// <summary>Fakturaya ödəniş əlavə edir. Ödəniş qalıq borcdan çox ola bilməz (domain invariantı).</summary>
public sealed record AddPaymentCommand(Guid InvoiceId, AddPaymentRequest Request) : IRequest<Result>;

public sealed class AddPaymentValidator : AbstractValidator<AddPaymentCommand>
{
    public AddPaymentValidator()
    {
        RuleFor(x => x.InvoiceId).NotEmpty();
        RuleFor(x => x.Request.Amount).GreaterThan(0);
        RuleFor(x => x.Request.Method).NotEmpty();
    }
}

public sealed class AddPaymentHandler(
    IInvoiceRepository invoices,
    IUnitOfWork unitOfWork)
    : IRequestHandler<AddPaymentCommand, Result>
{
    public async Task<Result> Handle(AddPaymentCommand request, CancellationToken ct)
    {
        var invoice = await invoices.GetByIdWithPaymentsAsync(request.InvoiceId, ct);
        if (invoice is null)
            return Result.Failure($"Faktura tapılmadı: {request.InvoiceId}");

        var dto = request.Request;
        var method = InvoiceMapping.ParseMethod(dto.Method);
        var amount = Money.Create(dto.Amount, invoice.TotalAmount.Currency);
        var paidAt = dto.PaidAt ?? DateOnly.FromDateTime(DateTime.UtcNow);

        var payment = invoice.AddPayment(amount, paidAt, method, dto.Note);

        // Yeni ödənişi açıq "Added" kimi izlə (client-set Guid açar səbəbindən EF onu
        // default halda "Modified" sayardı → yenilənmə 0 sətir → concurrency xətası).
        invoices.AttachNewPayment(payment);
        await unitOfWork.SaveChangesAsync(ct);

        return Result.Success();
    }
}
