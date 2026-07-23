using ERP.Application.Common.Interfaces;
using ERP.Application.Common.Messaging;
using ERP.Application.Common.Models;
using ERP.Shared.Contracts.Suppliers;

namespace ERP.Application.Modules.Suppliers.Queries;

/// <summary>Id-yə görə tək təchizatçı qaytarır (TDD §17 — Query).</summary>
public sealed record GetSupplierByIdQuery(Guid Id) : IRequest<Result<SupplierDto>>;

public sealed class GetSupplierByIdHandler(ISupplierRepository suppliers)
    : IRequestHandler<GetSupplierByIdQuery, Result<SupplierDto>>
{
    public async Task<Result<SupplierDto>> Handle(GetSupplierByIdQuery request, CancellationToken ct)
    {
        var supplier = await suppliers.GetByIdAsync(request.Id, ct);
        return supplier is null
            ? Result.Failure<SupplierDto>($"Təchizatçı tapılmadı: {request.Id}")
            : Result.Success(supplier.ToDto());
    }
}
