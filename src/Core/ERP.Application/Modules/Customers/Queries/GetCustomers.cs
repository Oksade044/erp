using ERP.Application.Common.Interfaces;
using ERP.Application.Common.Models;
using ERP.Shared.Contracts.Customers;
using MediatR;

namespace ERP.Application.Modules.Customers.Queries;

/// <summary>
/// Müştəriləri axtarış + səhifələmə ilə qaytarır (TDD §17 — Query, §11 server-side pagination).
/// </summary>
public sealed record GetCustomersQuery(string? Search, int Page = 1, int PageSize = 20)
    : IRequest<PagedResult<CustomerDto>>;

public sealed class GetCustomersHandler(ICustomerRepository customers)
    : IRequestHandler<GetCustomersQuery, PagedResult<CustomerDto>>
{
    public async Task<PagedResult<CustomerDto>> Handle(GetCustomersQuery request, CancellationToken ct)
    {
        var page = request.Page < 1 ? 1 : request.Page;
        var size = request.PageSize is < 1 or > 200 ? 20 : request.PageSize;

        var result = await customers.SearchAsync(request.Search, page, size, ct);

        return new PagedResult<CustomerDto>
        {
            Items = result.Items.Select(c => c.ToDto()).ToList(),
            TotalCount = result.TotalCount,
            Page = result.Page,
            PageSize = result.PageSize
        };
    }
}
