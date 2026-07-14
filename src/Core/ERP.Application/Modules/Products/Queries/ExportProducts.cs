using ERP.Application.Common.Interfaces;
using ERP.Application.Common.Messaging;

namespace ERP.Application.Modules.Products.Queries;

/// <summary>Bütün məhsulları xlsx bayt massivi kimi ixrac edir (TDD §26).</summary>
public sealed record ExportProductsQuery : IRequest<byte[]>;

public sealed class ExportProductsHandler(
    IProductRepository products,
    IExcelService excel)
    : IRequestHandler<ExportProductsQuery, byte[]>
{
    public async Task<byte[]> Handle(ExportProductsQuery request, CancellationToken ct)
    {
        var list = await products.ListAsync(ct);
        var dtos = list.Select(p => p.ToDto()).ToList();
        return excel.ExportProducts(dtos);
    }
}
