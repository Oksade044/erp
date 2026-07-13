using ERP.Shared.Contracts.Invoices;

namespace ERP.Application.Common.Interfaces;

/// <summary>
/// Faktura PDF generasiyası (TDD §25 — QuestPDF). İnterfeys Application-da, implementasiya
/// Infrastructure-da → Application PDF kitabxanasını tanımır.
/// </summary>
public interface IInvoicePdfService
{
    byte[] Generate(InvoiceDto invoice);
}
