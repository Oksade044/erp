using ERP.Application.Common.Interfaces;
using ERP.Application.Modules.Orders.Commands;
using ERP.Domain.Modules.Orders;
using ERP.Domain.Modules.Products;
using ERP.Domain.ValueObjects;
using NSubstitute;
using Xunit;

namespace ERP.Tests.Application;

public class ConfirmOrderHandlerTests
{
    private readonly IRentalOrderRepository _orders = Substitute.For<IRentalOrderRepository>();
    private readonly IProductRepository _products = Substitute.For<IProductRepository>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();

    private ConfirmOrderHandler Handler => new(_orders, _products, _uow);

    private static readonly Guid ProductId = Guid.NewGuid();

    private static (RentalOrder order, Product product) Setup(int lineQty, int stock)
    {
        var order = RentalOrder.Create("ORD-1", Guid.NewGuid(), "Toy Sarayı",
            new DateOnly(2026, 8, 1), new DateOnly(2026, 8, 3));
        order.AddLine(ProductId, "LED", lineQty, Money.Create(150m));

        var product = Product.Create(Sku.Create("LED-01"), "LED", Money.Create(150m),
            ProductTrackingMode.Nüsxə, stock);
        return (order, product);
    }

    [Fact]
    public async Task Rejects_when_not_enough_available_double_booking()
    {
        var (order, product) = Setup(lineQty: 5, stock: 8);
        _orders.GetByIdWithLinesAsync(order.Id).Returns(order);
        _products.GetByIdAsync(ProductId).Returns(product);
        // Başqa sifarişlərdə 5 ədəd rezerv olunub → mövcud yalnız 3.
        _orders.GetReservedQuantityAsync(ProductId, order.StartDate, order.EndDate, order.Id)
            .Returns(5);

        var result = await Handler.Handle(new ConfirmOrderCommand(order.Id), default);

        Assert.True(result.IsFailure);
        Assert.Equal(OrderStatus.Qaralama, order.Status);
        await _uow.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Confirms_when_enough_available()
    {
        var (order, product) = Setup(lineQty: 5, stock: 8);
        _orders.GetByIdWithLinesAsync(order.Id).Returns(order);
        _products.GetByIdAsync(ProductId).Returns(product);
        _orders.GetReservedQuantityAsync(ProductId, order.StartDate, order.EndDate, order.Id)
            .Returns(0);

        var result = await Handler.Handle(new ConfirmOrderCommand(order.Id), default);

        Assert.True(result.IsSuccess);
        Assert.Equal(OrderStatus.Təsdiqlənmiş, order.Status);
        await _uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
