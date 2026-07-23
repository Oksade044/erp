using ERP.Application.Common.Interfaces;
using ERP.Application.Modules.Purchases.Commands;
using ERP.Domain.Modules.Products;
using ERP.Domain.Modules.Purchases;
using ERP.Domain.ValueObjects;
using NSubstitute;
using Xunit;

namespace ERP.Tests.Application;

public class ReceivePurchaseHandlerTests
{
    private readonly IPurchaseOrderRepository _purchases = Substitute.For<IPurchaseOrderRepository>();
    private readonly IProductRepository _products = Substitute.For<IProductRepository>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();

    private ReceivePurchaseHandler Handler => new(_purchases, _products, _uow);

    [Fact]
    public async Task Receive_increments_product_stock()
    {
        var productId = Guid.NewGuid();
        var product = Product.Create(Sku.Create("SKU-1"), "Stul", Money.Create(5m),
            ProductTrackingMode.Toplu, stockQuantity: 3);

        var purchase = PurchaseOrder.Create("ALS-1", Guid.NewGuid(), "Dekor MMC",
            new DateOnly(2026, 7, 23));
        purchase.AddLine(productId, "Stul", 10, Money.Create(4m));
        purchase.Confirm();

        _purchases.GetByIdWithLinesAsync(purchase.Id).Returns(purchase);
        _products.GetByIdAsync(productId).Returns(product);

        var result = await Handler.Handle(new ReceivePurchaseCommand(purchase.Id), default);

        Assert.True(result.IsSuccess);
        Assert.Equal(PurchaseStatus.QəbulEdilmiş, purchase.Status);
        Assert.Equal(13, product.StockQuantity); // 3 + 10
        await _uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Receive_unconfirmed_fails()
    {
        var purchase = PurchaseOrder.Create("ALS-2", Guid.NewGuid(), "Dekor MMC",
            new DateOnly(2026, 7, 23));
        purchase.AddLine(Guid.NewGuid(), "Stul", 5, Money.Create(4m)); // still Qaralama

        _purchases.GetByIdWithLinesAsync(purchase.Id).Returns(purchase);

        await Assert.ThrowsAsync<ERP.Domain.Exceptions.DomainException>(
            () => Handler.Handle(new ReceivePurchaseCommand(purchase.Id), default));
    }
}
