using ERP.Shared.Contracts.Products;

namespace ERP.Application.Common.Interfaces;

/// <summary>
/// Excel idxal/ixrac (TDD §26 — ClosedXML). İnterfeys Application-da, implementasiya
/// Infrastructure-da → Application Excel kitabxanasını tanımır.
/// </summary>
public interface IExcelService
{
    /// <summary>Məhsulları xlsx bayt massivinə ixrac edir.</summary>
    byte[] ExportProducts(IReadOnlyList<ProductDto> products);

    /// <summary>xlsx axınından məhsul sətirlərini oxuyur (başlıq sətri gözlənilir).</summary>
    IReadOnlyList<CreateProductRequest> ParseProducts(Stream stream);
}
