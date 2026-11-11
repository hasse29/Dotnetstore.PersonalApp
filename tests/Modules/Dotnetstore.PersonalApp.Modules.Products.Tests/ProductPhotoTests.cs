using Dotnetstore.PersonalApp.Modules.Products.Features.Products;
using Dotnetstore.PersonalApp.Shared.Photos;
using SkiaSharp;

namespace Dotnetstore.PersonalApp.Modules.Products.Tests;

public sealed class ProductPhotoTests : IDisposable
{
    private static readonly byte[] Png = TestImage.Create(SKEncodedImageFormat.Png);

    private readonly ProductsFixture _fixture = new();

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public void Dispose() => _fixture.Dispose();

    private SetProductPhoto.Handler Set => new(_fixture.DbFactory);

    private GetProductPhoto.Handler Get => new(_fixture.DbFactory);

    private async Task<ProductDto> CreateProductAsync() =>
        (await _fixture.Create.HandleAsync(new CreateProduct.Command(ProductsFixture.Hardware()), Ct)).Value;

    [Fact]
    public async Task APhotoIsStoredReadBackListedAsAThumbnailAndRemoved()
    {
        var product = await CreateProductAsync();

        (await Set.HandleAsync(new SetProductPhoto.Command(product.Id, Png), Ct)).IsSuccess.ShouldBeTrue();

        (await Get.HandleAsync(new GetProductPhoto.Query(product.Id), Ct)).Value.ShouldBe(Png);
        var listed = (await _fixture.List.HandleAsync(new ListProducts.Query(), Ct)).Items.ShouldHaveSingleItem();
        listed.Thumbnail.ShouldNotBeNull();
        using var thumbnail = SKBitmap.Decode(listed.Thumbnail);
        thumbnail.Width.ShouldBe(PhotoThumbnail.Size);

        await Set.HandleAsync(new SetProductPhoto.Command(product.Id, null), Ct);
        (await Get.HandleAsync(new GetProductPhoto.Query(product.Id), Ct)).Value.ShouldBeNull();
        (await _fixture.List.HandleAsync(new ListProducts.Query(), Ct)).Items.ShouldHaveSingleItem().Thumbnail.ShouldBeNull();
    }

    [Fact]
    public async Task InvalidFilesAndUnknownProductsAreRejected()
    {
        var product = await CreateProductAsync();

        var notAnImage = await Set.HandleAsync(new SetProductPhoto.Command(product.Id, [1, 2, 3, 4]), Ct);
        var unknown = await Set.HandleAsync(new SetProductPhoto.Command(Guid.NewGuid(), Png), Ct);

        notAnImage.Error.Code.ShouldBe("photo_invalid");
        unknown.Error.Code.ShouldBe("product_not_found");
    }

    [Fact]
    public async Task UpdatingTheProductKeepsThePhoto()
    {
        var product = await CreateProductAsync();
        await Set.HandleAsync(new SetProductPhoto.Command(product.Id, Png), Ct);

        await _fixture.Update.HandleAsync(new UpdateProduct.Command(product.Id, ProductsFixture.Hardware() with { Name = "Renamed" }), Ct);

        (await Get.HandleAsync(new GetProductPhoto.Query(product.Id), Ct)).Value.ShouldBe(Png);
    }
}
