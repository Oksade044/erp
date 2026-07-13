using ERP.Application.Common.Interfaces;
using ERP.Application.Common.Messaging;
using ERP.Application.Common.Models;
using ERP.Shared.Contracts.Invoices;

namespace ERP.Application.Modules.Invoices.Queries;

/// <summary>Id-yə görə tək faktura (ödənişləri ilə). TDD §17.</summary>
public sealed record GetInvoiceByIdQuery(Guid Id) : IRequest<Result<InvoiceDto>>;

public sealed class GetInvoiceByIdHandler(IInvoiceRepository invoices)
    : IRequestHandler<GetInvoiceByIdQuery, Result<InvoiceDto>>
{
    public async Task<Result<InvoiceDto>> Handle(GetInvoiceByIdQuery request, CancellationToken ct)
    {
        var invoice = await invoices.GetByIdWithPaymentsAsync(request.Id, ct);
        return invoice is null
            ? Result.Failure<InvoiceDto>($"Faktura tapılmadı: {request.Id}")
            : Result.Success(invoice.ToDto());
    }
}
