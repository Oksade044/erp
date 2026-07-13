using ERP.Application.Common.Interfaces;
using ERP.Application.Common.Models;
using ERP.Shared.Contracts.Products;
using ERP.Application.Common.Messaging;

namespace ERP.Application.Modules.Products.Queries;

/// <summary>Id-yə görə tək məhsul qaytarır (TDD §17).</summary>
public sealed record GetProductByIdQuery(Guid Id) : IRequest<Result<ProductDto>>;

public sealed class GetProductByIdHandler(IProductRepository products)
    : IRequestHandler<GetProductByIdQuery, Result<ProductDto>>
{
    public async Task<Result<ProductDto>> Handle(GetProductByIdQuery request, CancellationToken ct)
    {
        var product = await products.GetByIdAsync(request.Id, ct);
        return product is null
            ? Result.Failure<ProductDto>($"Məhsul tapılmadı: {request.Id}")
            : Result.Success(product.ToDto());
    }
}
