using ERP.Application.Modules.Invoices.Commands;
using ERP.Application.Modules.Invoices.Queries;
using ERP.Application.Common.Messaging;
using ERP.Shared.Contracts.Invoices;

namespace ERP.Api.Endpoints;

/// <summary>
/// Faktura endpoint-ləri — versiyalı, RESTful (TDD §11). PDF qaimə yükləmə daxil (TDD §25).
/// </summary>
public static class InvoiceEndpoints
{
    public static IEndpointRouteBuilder MapInvoiceEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/invoices").WithTags("Invoices");

        group.MapGet("/", async (string? search, ISender sender, int page = 1, int pageSize = 20) =>
        {
            var result = await sender.Send(new GetInvoicesQuery(search, page, pageSize));
            return Results.Ok(result);
        });

        group.MapGet("/{id:guid}", async (Guid id, ISender sender) =>
        {
            var result = await sender.Send(new GetInvoiceByIdQuery(id));
            return result.IsSuccess ? Results.Ok(result.Value) : Results.NotFound(new { error = result.Error });
        });

        group.MapPost("/", async (CreateInvoiceRequest request, ISender sender) =>
        {
            var result = await sender.Send(new CreateInvoiceCommand(request));
            return result.IsSuccess
                ? Results.Created($"/api/v1/invoices/{result.Value}", new { id = result.Value })
                : Results.BadRequest(new { error = result.Error });
        });

        group.MapPost("/{id:guid}/payments", async (Guid id, AddPaymentRequest request, ISender sender) =>
        {
            var result = await sender.Send(new AddPaymentCommand(id, request));
            return result.IsSuccess ? Results.NoContent() : Results.BadRequest(new { error = result.Error });
        });

        group.MapGet("/{id:guid}/pdf", async (Guid id, ISender sender) =>
        {
            var result = await sender.Send(new GetInvoicePdfQuery(id));
            return result.IsSuccess
                ? Results.File(result.Value!, "application/pdf", $"faktura-{id}.pdf")
                : Results.NotFound(new { error = result.Error });
        });

        return app;
    }
}
