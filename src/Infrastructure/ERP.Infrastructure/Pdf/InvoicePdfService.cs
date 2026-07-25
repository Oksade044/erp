using ERP.Application.Common.Interfaces;
using ERP.Shared.Contracts.Invoices;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace ERP.Infrastructure.Pdf;

/// <summary>
/// QuestPDF ilə faktura (qaimə) PDF generasiyası (TDD §25). Kod-əsaslı, HTML-engine tələb etmir.
/// </summary>
public sealed class InvoicePdfService : IInvoicePdfService
{
    public byte[] Generate(InvoiceDto inv, IReadOnlyList<InvoicePdfLine> lines)
    {
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(40);
                page.DefaultTextStyle(t => t.FontSize(10).FontColor(Colors.Grey.Darken4));

                page.Header().Column(col =>
                {
                    col.Item().Text("QAİMƏ / FAKTURA").FontSize(20).Bold().FontColor(Colors.Blue.Darken2);
                    col.Item().Text($"№ {inv.InvoiceNumber}").FontSize(11).SemiBold();
                    col.Item().Text($"Tarix: {inv.IssueDate:dd.MM.yyyy}").FontColor(Colors.Grey.Darken1);
                });

                page.Content().PaddingVertical(15).Column(col =>
                {
                    col.Spacing(6);

                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text("Müştəri").SemiBold().FontColor(Colors.Grey.Darken1);
                            c.Item().Text(inv.CustomerName).FontSize(12);
                        });
                        row.RelativeItem().AlignRight().Column(c =>
                        {
                            c.Item().Text("Sifariş").SemiBold().FontColor(Colors.Grey.Darken1);
                            c.Item().Text(inv.OrderNumber).FontSize(12);
                        });
                    });

                    col.Item().PaddingTop(10).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);

                    // Məhsullar cədvəli (şəkil + ad + SKU + say + qiymət + məbləğ)
                    if (lines.Count > 0)
                    {
                        col.Item().PaddingTop(10).Text("Məhsullar").SemiBold().FontSize(12);
                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(c =>
                            {
                                c.ConstantColumn(52);   // şəkil
                                c.RelativeColumn(3);    // ad
                                c.RelativeColumn(2);    // sku
                                c.RelativeColumn(1);    // say
                                c.RelativeColumn(2);    // qiymət
                                c.RelativeColumn(2);    // məbləğ
                            });

                            table.Header(header =>
                            {
                                foreach (var h in new[] { "Şəkil", "Ad", "SKU", "Say", "Qiymət", "Məbləğ" })
                                    header.Cell().Background(Colors.Grey.Lighten3).Padding(5).Text(h).SemiBold();
                            });

                            foreach (var l in lines)
                            {
                                if (l.ImageBytes is { Length: > 0 } bytes)
                                    table.Cell().Padding(3).Height(44).AlignMiddle().Image(bytes).FitArea();
                                else
                                    table.Cell().Padding(5).AlignMiddle().AlignCenter().Text("—").FontColor(Colors.Grey.Medium);

                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(5).AlignMiddle().Text(l.ProductName);
                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(5).AlignMiddle().Text(l.Sku);
                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(5).AlignMiddle().Text(l.Quantity.ToString());
                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(5).AlignMiddle().Text($"{l.UnitPrice:0.00} {l.Currency}");
                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(5).AlignMiddle().Text($"{l.LineTotal:0.00} {l.Currency}");
                            }
                        });
                    }

                    // Məbləğ xülasəsi
                    col.Item().PaddingTop(10).Row(row =>
                    {
                        row.RelativeItem();
                        row.ConstantItem(230).Column(c =>
                        {
                            c.Item().Row(r =>
                            {
                                r.RelativeItem().Text("Ümumi məbləğ:").FontColor(Colors.Grey.Darken1);
                                r.AutoItem().Text($"{inv.TotalAmount:0.00} {inv.Currency}");
                            });
                            c.Item().Row(r =>
                            {
                                r.RelativeItem().Text("Ödənilib:").FontColor(Colors.Grey.Darken1);
                                r.AutoItem().Text($"{inv.AmountPaid:0.00} {inv.Currency}");
                            });
                            c.Item().PaddingVertical(3).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
                            c.Item().Row(r =>
                            {
                                r.RelativeItem().Text("Qalıq borc:").SemiBold();
                                r.AutoItem().Text($"{inv.Balance:0.00} {inv.Currency}").Bold();
                            });
                            c.Item().PaddingTop(4).AlignRight().Text($"Status: {inv.Status}")
                                .SemiBold().FontColor(Colors.Blue.Darken1);
                        });
                    });

                    // Ödəniş tarixçəsi
                    if (inv.Payments.Count > 0)
                    {
                        col.Item().PaddingTop(20).Text("Ödəniş tarixçəsi").SemiBold().FontSize(12);
                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(c =>
                            {
                                c.RelativeColumn(2);
                                c.RelativeColumn(2);
                                c.RelativeColumn(2);
                                c.RelativeColumn(3);
                            });

                            table.Header(header =>
                            {
                                foreach (var h in new[] { "Tarix", "Məbləğ", "Üsul", "Qeyd" })
                                    header.Cell().Background(Colors.Grey.Lighten3).Padding(5).Text(h).SemiBold();
                            });

                            foreach (var p in inv.Payments)
                            {
                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(5).Text($"{p.PaidAt:dd.MM.yyyy}");
                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(5).Text($"{p.Amount:0.00} {p.Currency}");
                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(5).Text(p.Method);
                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(5).Text(p.Note ?? "");
                            }
                        });
                    }
                });

                page.Footer().AlignCenter().Text(t =>
                {
                    t.Span("Toy Dekoru & Tədbir Avadanlığı İcarəsi").FontColor(Colors.Grey.Darken1);
                    t.Span("  •  ").FontColor(Colors.Grey.Lighten1);
                    t.Span($"Yaradıldı: {DateTime.Now:dd.MM.yyyy HH:mm}").FontColor(Colors.Grey.Medium);
                });
            });
        });

        return document.GeneratePdf();
    }
}
