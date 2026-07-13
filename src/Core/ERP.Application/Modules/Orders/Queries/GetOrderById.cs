using ERP.Application.Common.Interfaces;
using ERP.Application.Common.Models;
using ERP.Shared.Contracts.Orders;
using ERP.Application.Common.Messaging;

namespace ERP.Application.Modules.Orders.Queries;

/// <summary>Id-yə görə tək sifariş (sətirləri ilə). TDD §17.</summary>
public sealed record GetOrderByIdQuery(Guid Id) : IRequest<Result<OrderDto>>;

public sealed class GetOrderByIdHandler(IRentalOrderRepository orders)
    : IRequestHandler<GetOrderByIdQuery, Result<OrderDto>>
{
    public async Task<Result<OrderDto>> Handle(GetOrderByIdQuery request, CancellationToken ct)
    {
        var order = await orders.GetByIdWithLinesAsync(request.Id, ct);
        return order is null
            ? Result.Failure<OrderDto>($"Sifariş tapılmadı: {request.Id}")
            : Result.Success(order.ToDto());
    }
}
