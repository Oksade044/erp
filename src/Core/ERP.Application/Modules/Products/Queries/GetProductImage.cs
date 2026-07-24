using ERP.Application.Common.Interfaces;
using ERP.Application.Common.Messaging;
using ERP.Application.Common.Models;

namespace ERP.Application.Modules.Products.Queries;

/// <summary>Məhsulun şəkil məzmununu (baytlar) qaytarır — API stream kimi göndərir (TDD §24).</summary>
public sealed record GetProductImageQuery(Guid Id) : IRequest<Result<ProductImageResult>>;

/// <summary>Şəkil baytları + content-type (uzantıdan).</summary>
public sealed record ProductImageResult(byte[] Content, string ContentType);

public sealed class GetProductImageHandler(
    IProductRepository products,
    IFileStorage storage)
    : IRequestHandler<GetProductImageQuery, Result<ProductImageResult>>
{
    public async Task<Result<ProductImageResult>> Handle(GetProductImageQuery request, CancellationToken ct)
    {
        var product = await products.GetByIdAsync(request.Id, ct);
        if (product is null)
            return Result.Failure<ProductImageResult>($"Məhsul tapılmadı: {request.Id}");
        if (string.IsNullOrWhiteSpace(product.ImagePath))
            return Result.Failure<ProductImageResult>("Bu məhsulun şəkli yoxdur.");

        var stream = await storage.OpenReadAsync(product.ImagePath, ct);
        if (stream is null)
            return Result.Failure<ProductImageResult>("Şəkil faylı tapılmadı.");

        await using (stream)
        {
            using var ms = new MemoryStream();
            await stream.CopyToAsync(ms, ct);
            var ext = Path.GetExtension(product.ImagePath).ToLowerInvariant();
            var contentType = ext switch
            {
                ".png" => "image/png",
                ".webp" => "image/webp",
                ".gif" => "image/gif",
                _ => "image/jpeg"
            };
            return Result.Success(new ProductImageResult(ms.ToArray(), contentType));
        }
    }
}
