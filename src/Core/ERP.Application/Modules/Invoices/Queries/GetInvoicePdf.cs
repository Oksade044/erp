using ERP.Application.Common.Interfaces;
using ERP.Application.Common.Messaging;
using ERP.Application.Common.Models;

namespace ERP.Application.Modules.Invoices.Queries;

/// <summary>Fakturanın PDF qaiməsini bayt massivi kimi qaytarır (TDD §25).</summary>
public sealed record GetInvoicePdfQuery(Guid Id) : IRequest<Result<byte[]>>;

public sealed class GetInvoicePdfHandler(
    IInvoiceRepository invoices,
    IInvoicePdfService pdf)
    : IRequestHandler<GetInvoicePdfQuery, Result<byte[]>>
{
    public async Task<Result<byte[]>> Handle(GetInvoicePdfQuery request, CancellationToken ct)
    {
        var invoice = await invoices.GetByIdWithPaymentsAsync(request.Id, ct);
        if (invoice is null)
            return Result.Failure<byte[]>($"Faktura tapılmadı: {request.Id}");

        return Result.Success(pdf.Generate(invoice.ToDto()));
    }
}
