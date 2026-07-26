using ERP.Application.Common.Interfaces;
using ERP.Application.Common.Messaging;
using ERP.Shared.Contracts.Products;

namespace ERP.Application.Modules.Products.Queries;

/// <summary>Məhsulun anbarlar üzrə mövcudluğu (#18/#19) — sifariş yaradarkən göstərmək üçün.</summary>
public sealed record GetProductAvailabilityQuery(Guid ProductId) : IRequest<IReadOnlyList<ProductAvailabilityDto>>;

public sealed class GetProductAvailabilityHandler(IAvailabilityReader reader)
    : IRequestHandler<GetProductAvailabilityQuery, IReadOnlyList<ProductAvailabilityDto>>
{
    public Task<IReadOnlyList<ProductAvailabilityDto>> Handle(GetProductAvailabilityQuery request, CancellationToken ct) =>
        reader.GetProductAvailabilityAsync(request.ProductId, ct);
}
