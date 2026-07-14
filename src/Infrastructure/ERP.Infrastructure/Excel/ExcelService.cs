using ClosedXML.Excel;
using ERP.Application.Common.Interfaces;
using ERP.Shared.Contracts.Products;

namespace ERP.Infrastructure.Excel;

/// <summary>ClosedXML ilə məhsul Excel idxal/ixracı (TDD §26). Lisenziya problemi yoxdur.</summary>
public sealed class ExcelService : IExcelService
{
    private static readonly string[] Headers =
        ["SKU", "Ad", "Kateqoriya", "Təsvir", "İcarə qiyməti", "Valyuta", "İzləmə rejimi", "Anbar"];

    public byte[] ExportProducts(IReadOnlyList<ProductDto> products)
    {
        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Məhsullar");

        for (var i = 0; i < Headers.Length; i++)
        {
            var cell = ws.Cell(1, i + 1);
            cell.Value = Headers[i];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.LightGray;
        }

        var row = 2;
        foreach (var p in products)
        {
            ws.Cell(row, 1).Value = p.Sku;
            ws.Cell(row, 2).Value = p.Name;
            ws.Cell(row, 3).Value = p.Category;
            ws.Cell(row, 4).Value = p.Description;
            ws.Cell(row, 5).Value = p.RentalPrice;
            ws.Cell(row, 6).Value = p.Currency;
            ws.Cell(row, 7).Value = p.TrackingMode;
            ws.Cell(row, 8).Value = p.StockQuantity;
            row++;
        }

        ws.Columns().AdjustToContents();

        using var ms = new MemoryStream();
        workbook.SaveAs(ms);
        return ms.ToArray();
    }

    public IReadOnlyList<CreateProductRequest> ParseProducts(Stream stream)
    {
        using var workbook = new XLWorkbook(stream);
        var ws = workbook.Worksheets.First();
        var result = new List<CreateProductRequest>();

        // Başlıq sətrini ötür (row 1).
        foreach (var row in ws.RowsUsed().Skip(1))
        {
            var sku = row.Cell(1).GetString().Trim();
            if (string.IsNullOrWhiteSpace(sku))
                continue;

            result.Add(new CreateProductRequest(
                Sku: sku,
                Name: row.Cell(2).GetString().Trim(),
                RentalPrice: row.Cell(5).GetValue<decimal>(),
                TrackingMode: string.IsNullOrWhiteSpace(row.Cell(7).GetString()) ? "Toplu" : row.Cell(7).GetString().Trim(),
                Currency: string.IsNullOrWhiteSpace(row.Cell(6).GetString()) ? "AZN" : row.Cell(6).GetString().Trim(),
                StockQuantity: (int)row.Cell(8).GetValue<double>(),
                Category: row.Cell(3).GetString().Trim() is { Length: > 0 } cat ? cat : null,
                Description: row.Cell(4).GetString().Trim() is { Length: > 0 } desc ? desc : null));
        }

        return result;
    }
}
