using ERP.Application.Common.Interfaces;
using ERP.Application.Common.Messaging;
using ERP.Application.Common.Models;
using ERP.Shared.Contracts.Hr;

namespace ERP.Application.Modules.Hr.Queries;

/// <summary>Id-yə görə tək işçi qaytarır (TDD §17 — Query).</summary>
public sealed record GetEmployeeByIdQuery(Guid Id) : IRequest<Result<EmployeeDto>>;

public sealed class GetEmployeeByIdHandler(IEmployeeRepository employees)
    : IRequestHandler<GetEmployeeByIdQuery, Result<EmployeeDto>>
{
    public async Task<Result<EmployeeDto>> Handle(GetEmployeeByIdQuery request, CancellationToken ct)
    {
        var employee = await employees.GetByIdAsync(request.Id, ct);
        return employee is null
            ? Result.Failure<EmployeeDto>($"İşçi tapılmadı: {request.Id}")
            : Result.Success(employee.ToDto());
    }
}
