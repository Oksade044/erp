using ERP.Domain.Modules.Orders;
using ERP.Shared.Contracts.Orders;

namespace ERP.Application.Modules.Orders;

/// <summary>RentalOrder entity → DTO çevirmələri (TDD §12).</summary>
public static class OrderMapping
{
    public static OrderDto ToDto(this RentalOrder o) => new(
        Id: o.Id,
        OrderNumber: o.OrderNumber,
        CustomerId: o.CustomerId,
        CustomerName: o.CustomerName,
        StartDate: o.StartDate,
        EndDate: o.EndDate,
        Status: o.Status.ToString(),
        Notes: o.Notes,
        Total: o.Total.Amount,
        Currency: o.Total.Currency,
        Deposit: o.Deposit?.Amount ?? 0m,
        DamageCharge: o.DamageCharge?.Amount ?? 0m,
        PenaltyCharge: o.PenaltyCharge?.Amount ?? 0m,
        TotalCharges: o.TotalCharges.Amount,
        DepositRefund: o.DepositRefund.Amount,
        IsSettled: o.IsSettled,
        SettlementNotes: o.SettlementNotes,
        CreatedByName: o.CreatedByName,
        CreatedByRole: o.CreatedByRole,
        ConfirmedByName: o.ConfirmedByName,
        Lines: o.Lines.Select(l => new OrderLineDto(
            ProductId: l.ProductId,
            ProductName: l.ProductName,
            Quantity: l.Quantity,
            UnitPrice: l.UnitPrice.Amount,
            LineTotal: l.LineTotal.Amount,
            Currency: l.UnitPrice.Currency)).ToList());
}
