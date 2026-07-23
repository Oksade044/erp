using ERP.Application.Common.Interfaces;
using ERP.Application.Common.Messaging;
using ERP.Application.Common.Models;
using ERP.Shared.Contracts.Hr;

namespace ERP.Application.Modules.Hr.Queries;

/// <summary>Əməkhaqqı hesablamalarını axtarış + işçi filtri + səhifələmə ilə qaytarır (TDD §17, §11).</summary>
public sealed record GetPayrollsQuery(string? Search, Guid? EmployeeId, int Page = 1, int PageSize = 20)
    : IRequest<PagedResult<PayrollDto>>;

public sealed class GetPayrollsHandler(IPayrollRepository payrolls)
    : IRequestHandler<GetPayrollsQuery, PagedResult<PayrollDto>>
{
    public async Task<PagedResult<PayrollDto>> Handle(GetPayrollsQuery request, CancellationToken ct)
    {
        var page = request.Page < 1 ? 1 : request.Page;
        var size = request.PageSize is < 1 or > 200 ? 20 : request.PageSize;

        var result = await payrolls.SearchAsync(request.Search, request.EmployeeId, page, size, ct);

        return new PagedResult<PayrollDto>
        {
            Items = result.Items.Select(p => p.ToDto()).ToList(),
            TotalCount = result.TotalCount,
            Page = result.Page,
            PageSize = result.PageSize
        };
    }
}
