using ERP.Application.Common.Interfaces;
using ERP.Application.Common.Messaging;
using ERP.Application.Common.Models;

namespace ERP.Application.Modules.Products.Queries;

/// <summary>Məhsulun QR kodunu PNG kimi qaytarır (SKU kodlaşdırılır). TDD §27.</summary>
public sealed record GetProductQrQuery(Guid Id) : IRequest<Result<byte[]>>;

public sealed class GetProductQrHandler(
    IProductRepository products,
    IBarcodeService barcodes)
    : IRequestHandler<GetProductQrQuery, Result<byte[]>>
{
    public async Task<Result<byte[]>> Handle(GetProductQrQuery request, CancellationToken ct)
    {
        var product = await products.GetByIdAsync(request.Id, ct);
        return product is null
            ? Result.Failure<byte[]>($"Məhsul tapılmadı: {request.Id}")
            : Result.Success(barcodes.GenerateQrPng(product.Sku.Value));
    }
}
