using ERP.Application.Common.Interfaces;
using ERP.Application.Common.Messaging;
using ERP.Application.Common.Models;
using ERP.Domain.Exceptions;
using ERP.Domain.Modules.Products;
using ERP.Domain.ValueObjects;
using ERP.Shared.Contracts.Products;

namespace ERP.Application.Modules.Products.Commands;

/// <summary>
/// xlsx fayldan məhsulları toplu idxal edir (TDD §26). Mövcud SKU-lar ötürülür, yanlış
/// sətirlər xəta siyahısına yazılır; qalan hamısı bir transaction-da saxlanılır.
/// </summary>
public sealed record ImportProductsCommand(byte[] FileBytes) : IRequest<Result<ImportResultDto>>;

public sealed class ImportProductsHandler(
    IProductRepository products,
    IExcelService excel,
    IUnitOfWork unitOfWork)
    : IRequestHandler<ImportProductsCommand, Result<ImportResultDto>>
{
    public async Task<Result<ImportResultDto>> Handle(ImportProductsCommand request, CancellationToken ct)
    {
        List<CreateProductRequest> rows;
        try
        {
            using var stream = new MemoryStream(request.FileBytes);
            rows = [.. excel.ParseProducts(stream)];
        }
        catch (Exception ex)
        {
            return Result.Failure<ImportResultDto>($"Fayl oxunmadı: {ex.Message}");
        }

        var created = 0;
        var skipped = 0;
        var errors = new List<string>();
        var rowNo = 1;

        foreach (var dto in rows)
        {
            rowNo++;
            try
            {
                var sku = Sku.Create(dto.Sku);
                if (await products.SkuExistsAsync(sku.Value, ct))
                {
                    skipped++;
                    continue;
                }

                var mode = ProductMapping.ParseTrackingMode(dto.TrackingMode);
                var price = Money.Create(dto.RentalPrice, dto.Currency);
                var product = Product.Create(sku, dto.Name, price, mode, dto.StockQuantity, dto.Category, dto.Description);

                await products.AddAsync(product, ct);
                created++;
            }
            catch (DomainException ex)
            {
                errors.Add($"Sətir {rowNo}: {ex.Message}");
            }
        }

        if (created > 0)
            await unitOfWork.SaveChangesAsync(ct);

        return Result.Success(new ImportResultDto(created, skipped, errors));
    }
}
