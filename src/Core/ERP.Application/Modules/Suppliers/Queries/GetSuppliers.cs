using ERP.Application.Common.Interfaces;
using ERP.Application.Common.Messaging;
using ERP.Application.Common.Models;
using ERP.Shared.Contracts.Suppliers;

namespace ERP.Application.Modules.Suppliers.Queries;

/// <summary>Təchizatçıları axtarış + səhifələmə ilə qaytarır (TDD §17, §11).</summary>
public sealed record GetSuppliersQuery(string? Search, int Page = 1, int PageSize = 20)
    : IRequest<PagedResult<SupplierDto>>;

public sealed class GetSuppliersHandler(ISupplierRepository suppliers)
    : IRequestHandler<GetSuppliersQuery, PagedResult<SupplierDto>>
{
    public async Task<PagedResult<SupplierDto>> Handle(GetSuppliersQuery request, CancellationToken ct)
    {
        var page = request.Page < 1 ? 1 : request.Page;
        var size = request.PageSize is < 1 or > 200 ? 20 : request.PageSize;

        var result = await suppliers.SearchAsync(request.Search, page, size, ct);

        return new PagedResult<SupplierDto>
        {
            Items = result.Items.Select(s => s.ToDto()).ToList(),
            TotalCount = result.TotalCount,
            Page = result.Page,
            PageSize = result.PageSize
        };
    }
}
