using ERP.Application.Common.Interfaces;
using ERP.Application.Common.Messaging;
using ERP.Application.Common.Models;

namespace ERP.Application.Modules.Invoices.Queries;

/// <summary>Fakturanın PDF qaiməsini bayt massivi kimi qaytarır (TDD §25).</summary>
public sealed record GetInvoicePdfQuery(Guid Id) : IRequest<Result<byte[]>>;

public sealed class GetInvoicePdfHandler(
    IInvoiceRepository invoices,
    IRentalOrderRepository orders,
    IProductRepository products,
    IFileStorage fileStorage,
    IInvoicePdfService pdf)
    : IRequestHandler<GetInvoicePdfQuery, Result<byte[]>>
{
    public async Task<Result<byte[]>> Handle(GetInvoicePdfQuery request, CancellationToken ct)
    {
        var invoice = await invoices.GetByIdWithPaymentsAsync(request.Id, ct);
        if (invoice is null)
            return Result.Failure<byte[]>($"Faktura tapılmadı: {request.Id}");

        // Fakturanın bağlı olduğu sifarişin sətirlərini + hər məhsulun şəklini gətir (PDF üçün).
        var lines = new List<InvoicePdfLine>();
        var order = await orders.GetByIdWithLinesAsync(invoice.OrderId, ct);
        if (order is not null)
        {
            foreach (var line in order.Lines)
            {
                var product = await products.GetByIdAsync(line.ProductId, ct);
                byte[]? imageBytes = null;
                if (product?.ImagePath is { Length: > 0 } key)
                    imageBytes = await ReadImageAsync(key, ct);

                lines.Add(new InvoicePdfLine(
                    ProductName: line.ProductName,
                    Sku: product?.Sku.Value ?? "",
                    Quantity: line.Quantity,
                    UnitPrice: line.UnitPrice.Amount,
                    LineTotal: line.LineTotal.Amount,
                    Currency: line.UnitPrice.Currency,
                    ImageBytes: imageBytes));
            }
        }

        return Result.Success(pdf.Generate(invoice.ToDto(), lines));
    }

    private async Task<byte[]?> ReadImageAsync(string key, CancellationToken ct)
    {
        var stream = await fileStorage.OpenReadAsync(key, ct);
        if (stream is null) return null;
        await using (stream)
        {
            using var ms = new MemoryStream();
            await stream.CopyToAsync(ms, ct);
            return ms.ToArray();
        }
    }
}
