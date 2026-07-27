using ERP.Application.Common.Interfaces;
using ERP.Application.Modules.Purchases.Commands;
using ERP.Domain.Modules.Products;
using ERP.Domain.Modules.Purchases;
using ERP.Domain.Modules.Warehouses;
using ERP.Domain.ValueObjects;
using NSubstitute;
using Xunit;

namespace ERP.Tests.Application;

public class ReceivePurchaseHandlerTests
{
    private readonly IPurchaseOrderRepository _purchases = Substitute.For<IPurchaseOrderRepository>();
    private readonly IProductRepository _products = Substitute.For<IProductRepository>();
    private readonly IStockLevelRepository _stockLevels = Substitute.For<IStockLevelRepository>();
    private readonly IWarehouseRepository _warehouses = Substitute.For<IWarehouseRepository>();
    private readonly IStockNotifier _notifier = Substitute.For<IStockNotifier>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();

    private ReceivePurchaseHandler Handler => new(_purchases, _products, _stockLevels, _warehouses, _notifier, _uow);

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
    public async Task Receive_with_warehouse_increments_stock_level()
    {
        var productId = Guid.NewGuid();
        var product = Product.Create(Sku.Create("SKU-2"), "Masa", Money.Create(15m),
            ProductTrackingMode.Toplu, stockQuantity: 0);
        var warehouse = Warehouse.Create("Mərkəzi", "MRK");

        var purchase = PurchaseOrder.Create("ALS-3", Guid.NewGuid(), "Dekor MMC",
            new DateOnly(2026, 7, 23), warehouseId: warehouse.Id);
        purchase.AddLine(productId, "Masa", 20, Money.Create(80m));
        purchase.Confirm();

        _purchases.GetByIdWithLinesAsync(purchase.Id).Returns(purchase);
        _products.GetByIdAsync(productId).Returns(product);
        _warehouses.GetByIdAsync(warehouse.Id).Returns(warehouse);
        _stockLevels.GetAsync(productId, warehouse.Id).Returns((StockLevel?)null); // yeni səviyyə yaranır

        var result = await Handler.Handle(new ReceivePurchaseCommand(purchase.Id), default);

        Assert.True(result.IsSuccess);
        // Anbarda StockLevel yaradıldı (say 20).
        await _stockLevels.Received(1).AddAsync(
            Arg.Is<StockLevel>(s => s.WarehouseId == warehouse.Id && s.Quantity == 20),
            Arg.Any<CancellationToken>());
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
