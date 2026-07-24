using ERP.Application.Common.Interfaces;
using ERP.Application.Modules.Products.Commands;
using ERP.Domain.Modules.Products;
using ERP.Domain.ValueObjects;
using NSubstitute;
using Xunit;

namespace ERP.Tests.Application;

public class SetProductImageHandlerTests
{
    private readonly IProductRepository _products = Substitute.For<IProductRepository>();
    private readonly IFileStorage _storage = Substitute.For<IFileStorage>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();

    private SetProductImageHandler Handler => new(_products, _storage, _uow);

    private static Product NewProduct() =>
        Product.Create(Sku.Create("SKU-IMG"), "Kraliça stulu", Money.Create(10m), ProductTrackingMode.Toplu);

    [Fact]
    public async Task Valid_image_is_stored_and_path_set()
    {
        var product = NewProduct();
        _products.GetByIdAsync(product.Id).Returns(product);
        _storage.SaveAsync(Arg.Any<Stream>(), "products", ".png", Arg.Any<CancellationToken>())
            .Returns("products/abc.png");

        var result = await Handler.Handle(
            new SetProductImageCommand(product.Id, new byte[] { 1, 2, 3 }, ".png"), default);

        Assert.True(result.IsSuccess);
        Assert.Equal("products/abc.png", product.ImagePath);
        await _uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Unsupported_extension_fails()
    {
        var product = NewProduct();
        _products.GetByIdAsync(product.Id).Returns(product);

        var result = await Handler.Handle(
            new SetProductImageCommand(product.Id, new byte[] { 1 }, ".exe"), default);

        Assert.True(result.IsFailure);
        await _uow.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Replacing_image_deletes_old_file()
    {
        var product = NewProduct();
        product.SetImagePath("products/old.jpg");
        _products.GetByIdAsync(product.Id).Returns(product);
        _storage.SaveAsync(Arg.Any<Stream>(), "products", ".jpg", Arg.Any<CancellationToken>())
            .Returns("products/new.jpg");

        var result = await Handler.Handle(
            new SetProductImageCommand(product.Id, new byte[] { 9 }, ".jpg"), default);

        Assert.True(result.IsSuccess);
        await _storage.Received(1).DeleteAsync("products/old.jpg", Arg.Any<CancellationToken>());
    }
}
