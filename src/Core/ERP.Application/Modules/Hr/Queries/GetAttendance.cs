using ERP.Application.Common.Interfaces;
using ERP.Application.Common.Messaging;
using ERP.Application.Common.Models;
using ERP.Shared.Contracts.Hr;

namespace ERP.Application.Modules.Hr.Queries;

/// <summary>Davamiyyət qeydlərini axtarış + işçi filtri + səhifələmə ilə qaytarır (TDD §17, §11).</summary>
public sealed record GetAttendanceQuery(string? Search, Guid? EmployeeId, int Page = 1, int PageSize = 20)
    : IRequest<PagedResult<AttendanceDto>>;

public sealed class GetAttendanceHandler(IAttendanceRepository attendance)
    : IRequestHandler<GetAttendanceQuery, PagedResult<AttendanceDto>>
{
    public async Task<PagedResult<AttendanceDto>> Handle(GetAttendanceQuery request, CancellationToken ct)
    {
        var page = request.Page < 1 ? 1 : request.Page;
        var size = request.PageSize is < 1 or > 200 ? 20 : request.PageSize;

        var result = await attendance.SearchAsync(request.Search, request.EmployeeId, page, size, ct);

        return new PagedResult<AttendanceDto>
        {
            Items = result.Items.Select(a => a.ToDto()).ToList(),
            TotalCount = result.TotalCount,
            Page = result.Page,
            PageSize = result.PageSize
        };
    }
}
