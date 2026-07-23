using ERP.Application.Common.Interfaces;
using ERP.Application.Common.Messaging;
using ERP.Application.Common.Models;
using ERP.Shared.Contracts.Hr;

namespace ERP.Application.Modules.Hr.Queries;

/// <summary>Id-yə görə tək əməkhaqqı hesablaması qaytarır (TDD §17 — Query).</summary>
public sealed record GetPayrollByIdQuery(Guid Id) : IRequest<Result<PayrollDto>>;

public sealed class GetPayrollByIdHandler(IPayrollRepository payrolls)
    : IRequestHandler<GetPayrollByIdQuery, Result<PayrollDto>>
{
    public async Task<Result<PayrollDto>> Handle(GetPayrollByIdQuery request, CancellationToken ct)
    {
        var payroll = await payrolls.GetByIdAsync(request.Id, ct);
        return payroll is null
            ? Result.Failure<PayrollDto>($"Əməkhaqqı tapılmadı: {request.Id}")
            : Result.Success(payroll.ToDto());
    }
}
