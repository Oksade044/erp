using ERP.Shared.Contracts.Invoices;

namespace ERP.Application.Common.Interfaces;

/// <summary>Faktura PDF-ində göstərilən məhsul sətri (şəkil daxil). Domenə asılılıq yoxdur.</summary>
public sealed record InvoicePdfLine(
    string ProductName,
    string Sku,
    int Quantity,
    decimal UnitPrice,
    decimal LineTotal,
    string Currency,
    byte[]? ImageBytes);

/// <summary>
/// Faktura PDF generasiyası (TDD §25 — QuestPDF). İnterfeys Application-da, implementasiya
/// Infrastructure-da → Application PDF kitabxanasını tanımır. Sətirlərdə məhsul şəkli də verilir.
/// </summary>
public interface IInvoicePdfService
{
    byte[] Generate(InvoiceDto invoice, IReadOnlyList<InvoicePdfLine> lines);
}
