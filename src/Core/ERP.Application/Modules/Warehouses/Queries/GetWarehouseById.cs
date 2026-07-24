using ERP.Application.Common.Interfaces;
using ERP.Application.Common.Messaging;
using ERP.Application.Common.Models;
using ERP.Shared.Contracts.Warehouses;

namespace ERP.Application.Modules.Warehouses.Queries;

/// <summary>Id-yə görə tək anbar qaytarır (TDD §17 — Query).</summary>
public sealed record GetWarehouseByIdQuery(Guid Id) : IRequest<Result<WarehouseDto>>;

public sealed class GetWarehouseByIdHandler(IWarehouseRepository warehouses)
    : IRequestHandler<GetWarehouseByIdQuery, Result<WarehouseDto>>
{
    public async Task<Result<WarehouseDto>> Handle(GetWarehouseByIdQuery request, CancellationToken ct)
    {
        var warehouse = await warehouses.GetByIdAsync(request.Id, ct);
        return warehouse is null
            ? Result.Failure<WarehouseDto>($"Anbar tapılmadı: {request.Id}")
            : Result.Success(warehouse.ToDto());
    }
}
