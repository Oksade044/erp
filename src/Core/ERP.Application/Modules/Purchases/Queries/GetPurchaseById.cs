using ERP.Application.Common.Interfaces;
using ERP.Application.Common.Messaging;
using ERP.Application.Common.Models;
using ERP.Shared.Contracts.Purchases;

namespace ERP.Application.Modules.Purchases.Queries;

/// <summary>Id-yə görə tək alışı sətirləri ilə qaytarır (TDD §17 — Query).</summary>
public sealed record GetPurchaseByIdQuery(Guid Id) : IRequest<Result<PurchaseDto>>;

public sealed class GetPurchaseByIdHandler(IPurchaseOrderRepository purchases)
    : IRequestHandler<GetPurchaseByIdQuery, Result<PurchaseDto>>
{
    public async Task<Result<PurchaseDto>> Handle(GetPurchaseByIdQuery request, CancellationToken ct)
    {
        var purchase = await purchases.GetByIdWithLinesAsync(request.Id, ct);
        return purchase is null
            ? Result.Failure<PurchaseDto>($"Alış tapılmadı: {request.Id}")
            : Result.Success(purchase.ToDto());
    }
}
