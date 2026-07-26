using ERP.Application.Common.Interfaces;
using ERP.Application.Common.Messaging;
using ERP.Shared.Contracts.Products;

namespace ERP.Application.Modules.Products.Queries;

/// <summary>Məhsulun istifadə tarixçəsi (#38).</summary>
public sealed record GetProductHistoryQuery(Guid ProductId) : IRequest<IReadOnlyList<ProductHistoryRowDto>>;

public sealed class GetProductHistoryHandler(IProductHistoryReader reader)
    : IRequestHandler<GetProductHistoryQuery, IReadOnlyList<ProductHistoryRowDto>>
{
    public Task<IReadOnlyList<ProductHistoryRowDto>> Handle(GetProductHistoryQuery request, CancellationToken ct) =>
        reader.GetAsync(request.ProductId, ct);
}
