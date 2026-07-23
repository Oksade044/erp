using ERP.Domain.Modules.Purchases;
using ERP.Shared.Contracts.Purchases;

namespace ERP.Application.Modules.Purchases;

/// <summary>PurchaseOrder entity → DTO çevirmələri (TDD §12).</summary>
public static class PurchaseMapping
{
    public static PurchaseDto ToDto(this PurchaseOrder p) => new(
        Id: p.Id,
        PurchaseNumber: p.PurchaseNumber,
        SupplierId: p.SupplierId,
        SupplierName: p.SupplierName,
        OrderDate: p.OrderDate,
        Status: p.Status.ToString(),
        Total: p.Total.Amount,
        Currency: p.Total.Currency,
        Notes: p.Notes,
        CreatedAt: p.CreatedAt,
        Lines: p.Lines.Select(l => new PurchaseLineDto(
            l.ProductId, l.ProductName, l.Quantity, l.UnitCost.Amount, l.LineTotal.Amount)).ToList());
}
