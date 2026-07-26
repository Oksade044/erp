using ERP.Application.Common.Interfaces;
using ERP.Application.Modules.Orders.Commands;
using ERP.Domain.Modules.Orders;
using ERP.Domain.ValueObjects;
using ERP.Shared.Contracts.Products;
using NSubstitute;
using Xunit;

namespace ERP.Tests.Application;

public class ConfirmOrderHandlerTests
{
    private readonly IRentalOrderRepository _orders = Substitute.For<IRentalOrderRepository>();
    private readonly IInvoiceRepository _invoices = Substitute.For<IInvoiceRepository>();
    private readonly IAvailabilityReader _availability = Substitute.For<IAvailabilityReader>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();

    private ConfirmOrderHandler Handler => new(_orders, _invoices, _availability, _currentUser, _uow);

    private static readonly Guid ProductId = Guid.NewGuid();

    private static RentalOrder Setup(int lineQty)
    {
        var order = RentalOrder.Create("ORD-1", Guid.NewGuid(), "Toy Sarayı",
            new DateOnly(2026, 8, 1), new DateOnly(2026, 8, 3));
        order.AddLine(ProductId, "LED", lineQty, Money.Create(150m));
        return order;
    }

    private void FreeStock(int free) =>
        _availability.GetProductAvailabilityAsync(ProductId, Arg.Any<CancellationToken>())
            .Returns(new List<ProductAvailabilityDto>
            {
                new(Guid.NewGuid(), "Anbar", Total: free, Reserved: 0, Rented: 0, Free: free)
            });

    [Fact]
    public async Task Rejects_when_not_enough_free_stock()
    {
        var order = Setup(lineQty: 5);
        _orders.GetByIdWithLinesAsync(order.Id).Returns(order);
        FreeStock(3); // yalnız 3 boş, tələb 5

        var result = await Handler.Handle(new ConfirmOrderCommand(order.Id), default);

        Assert.True(result.IsFailure);
        Assert.Equal(OrderStatus.Qaralama, order.Status);
        await _uow.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Confirms_when_enough_free_stock()
    {
        var order = Setup(lineQty: 5);
        _orders.GetByIdWithLinesAsync(order.Id).Returns(order);
        FreeStock(8);

        var result = await Handler.Handle(new ConfirmOrderCommand(order.Id), default);

        Assert.True(result.IsSuccess);
        Assert.Equal(OrderStatus.Təsdiqlənmiş, order.Status);
        await _uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Auto_creates_invoice_on_confirm()
    {
        var order = Setup(lineQty: 2);
        _orders.GetByIdWithLinesAsync(order.Id).Returns(order);
        FreeStock(10);
        _invoices.ExistsForOrderAsync(order.Id).Returns(false);

        var result = await Handler.Handle(new ConfirmOrderCommand(order.Id), default);

        Assert.True(result.IsSuccess);
        await _invoices.Received(1).AddAsync(
            Arg.Is<ERP.Domain.Modules.Invoices.Invoice>(i => i.OrderId == order.Id),
            Arg.Any<CancellationToken>());
    }
}
