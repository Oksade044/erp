using ERP.Application.Common.Interfaces;
using ERP.Application.Common.Messaging;
using ERP.Application.Common.Models;
using ERP.Shared.Contracts.Hr;

namespace ERP.Application.Modules.Hr.Queries;

/// <summary>İşçiləri axtarış + səhifələmə ilə qaytarır (TDD §17, §11).</summary>
public sealed record GetEmployeesQuery(string? Search, int Page = 1, int PageSize = 20)
    : IRequest<PagedResult<EmployeeDto>>;

public sealed class GetEmployeesHandler(IEmployeeRepository employees)
    : IRequestHandler<GetEmployeesQuery, PagedResult<EmployeeDto>>
{
    public async Task<PagedResult<EmployeeDto>> Handle(GetEmployeesQuery request, CancellationToken ct)
    {
        var page = request.Page < 1 ? 1 : request.Page;
        var size = request.PageSize is < 1 or > 200 ? 20 : request.PageSize;

        var result = await employees.SearchAsync(request.Search, page, size, ct);

        return new PagedResult<EmployeeDto>
        {
            Items = result.Items.Select(e => e.ToDto()).ToList(),
            TotalCount = result.TotalCount,
            Page = result.Page,
            PageSize = result.PageSize
        };
    }
}
