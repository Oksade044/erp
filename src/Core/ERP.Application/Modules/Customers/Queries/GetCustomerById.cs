using ERP.Application.Common.Interfaces;
using ERP.Application.Common.Models;
using ERP.Shared.Contracts.Customers;
using MediatR;

namespace ERP.Application.Modules.Customers.Queries;

/// <summary>Id-yə görə tək müştəri qaytarır (TDD §17 — Query).</summary>
public sealed record GetCustomerByIdQuery(Guid Id) : IRequest<Result<CustomerDto>>;

public sealed class GetCustomerByIdHandler(ICustomerRepository customers)
    : IRequestHandler<GetCustomerByIdQuery, Result<CustomerDto>>
{
    public async Task<Result<CustomerDto>> Handle(GetCustomerByIdQuery request, CancellationToken ct)
    {
        var customer = await customers.GetByIdAsync(request.Id, ct);
        return customer is null
            ? Result.Failure<CustomerDto>($"Müştəri tapılmadı: {request.Id}")
            : Result.Success(customer.ToDto());
    }
}
