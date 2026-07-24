using ERP.Application.Common.Interfaces;
using ERP.Application.Common.Messaging;
using ERP.Application.Common.Models;

namespace ERP.Application.Modules.Products.Commands;

/// <summary>
/// Məhsula şəkil yükləyir: faylı storage-ə yazır, köhnə şəkli silir, məhsulun ImagePath-ini
/// yeniləyir. Fayl DB-də deyil, storage-də saxlanır (TDD §24).
/// </summary>
public sealed record SetProductImageCommand(Guid ProductId, byte[] Content, string Extension)
    : IRequest<Result<string>>;

public sealed class SetProductImageHandler(
    IProductRepository products,
    IFileStorage storage,
    IUnitOfWork unitOfWork)
    : IRequestHandler<SetProductImageCommand, Result<string>>
{
    private static readonly string[] Allowed = [".jpg", ".jpeg", ".png", ".webp", ".gif"];

    public async Task<Result<string>> Handle(SetProductImageCommand request, CancellationToken ct)
    {
        var ext = request.Extension.StartsWith('.') ? request.Extension : "." + request.Extension;
        ext = ext.ToLowerInvariant();
        if (!Allowed.Contains(ext))
            return Result.Failure<string>($"Dəstəklənməyən şəkil formatı: {ext}. (jpg/png/webp/gif)");
        if (request.Content.Length == 0)
            return Result.Failure<string>("Boş fayl.");

        var product = await products.GetByIdAsync(request.ProductId, ct);
        if (product is null)
            return Result.Failure<string>($"Məhsul tapılmadı: {request.ProductId}");

        var oldKey = product.ImagePath;

        using var ms = new MemoryStream(request.Content);
        var key = await storage.SaveAsync(ms, "products", ext, ct);

        product.SetImagePath(key);
        products.Update(product);
        await unitOfWork.SaveChangesAsync(ct);

        // Köhnə şəkli təmizlə (yeni uğurla saxlanandan sonra).
        if (!string.IsNullOrWhiteSpace(oldKey))
            await storage.DeleteAsync(oldKey, ct);

        return Result.Success(key);
    }
}
