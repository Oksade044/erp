using ERP.Application.Common.Interfaces;
using ERP.Application.Common.Messaging;
using ERP.Application.Common.Models;
using ERP.Shared.Contracts.Invoices;

namespace ERP.Application.Modules.Invoices.Queries;

/// <summary>Fakturaları axtarış + səhifələmə ilə qaytarır (TDD §11, §17).</summary>
public sealed record GetInvoicesQuery(string? Search, int Page = 1, int PageSize = 20)
    : IRequest<PagedResult<InvoiceDto>>;

public sealed class GetInvoicesHandler(IInvoiceRepository invoices)
    : IRequestHandler<GetInvoicesQuery, PagedResult<InvoiceDto>>
{
    public async Task<PagedResult<InvoiceDto>> Handle(GetInvoicesQuery request, CancellationToken ct)
    {
        var page = request.Page < 1 ? 1 : request.Page;
        var size = request.PageSize is < 1 or > 200 ? 20 : request.PageSize;

        var result = await invoices.SearchAsync(request.Search, page, size, ct);

        return new PagedResult<InvoiceDto>
        {
            Items = result.Items.Select(i => i.ToDto()).ToList(),
            TotalCount = result.TotalCount,
            Page = result.Page,
            PageSize = result.PageSize
        };
    }
}
