using Dotnetstore.PersonalApp.Modules.Sales.Features.Customers;
using Dotnetstore.PersonalApp.Shared.Contracts.Events;
using Dotnetstore.PersonalApp.Shared.Photos;
using SkiaSharp;

namespace Dotnetstore.PersonalApp.Modules.Sales.Tests;

public sealed class CustomerPhotoTests
{
    private static readonly byte[] Png = TestImage.Create(SKEncodedImageFormat.Png);

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task APhotoIsStoredReadBackListedAsAThumbnailAndRemoved()
    {
        using var fixture = new SalesFixture();
        var customer = await fixture.NewCustomerAsync();
        var set = new SetCustomerPhoto.Handler(fixture.DbFactory);
        var get = new GetCustomerPhoto.Handler(fixture.DbFactory);

        (await set.HandleAsync(new SetCustomerPhoto.Command(customer.Id, Png), Ct)).IsSuccess.ShouldBeTrue();

        (await get.HandleAsync(new GetCustomerPhoto.Query(customer.Id), Ct)).Value.ShouldBe(Png);
        var listed = (await fixture.ListCustomers.HandleAsync(new ListCustomers.Query(), Ct)).Items.Single();
        listed.Thumbnail.ShouldNotBeNull();
        using var thumbnail = SKBitmap.Decode(listed.Thumbnail);
        thumbnail.Width.ShouldBe(PhotoThumbnail.Size);

        await set.HandleAsync(new SetCustomerPhoto.Command(customer.Id, null), Ct);
        (await get.HandleAsync(new GetCustomerPhoto.Query(customer.Id), Ct)).Value.ShouldBeNull();
        (await fixture.ListCustomers.HandleAsync(new ListCustomers.Query(), Ct)).Items.Single().Thumbnail.ShouldBeNull();
    }

    [Fact]
    public async Task InvalidFilesAndUnknownCustomersAreRejected()
    {
        using var fixture = new SalesFixture();
        var customer = await fixture.NewCustomerAsync();
        var set = new SetCustomerPhoto.Handler(fixture.DbFactory);

        (await set.HandleAsync(new SetCustomerPhoto.Command(customer.Id, [1, 2, 3, 4]), Ct)).Error.Code.ShouldBe("photo_invalid");
        (await set.HandleAsync(new SetCustomerPhoto.Command(Guid.NewGuid(), Png), Ct)).Error.Code.ShouldBe("customer_not_found");
    }

    [Fact]
    public async Task UpdatingTheCustomerKeepsThePhoto()
    {
        using var fixture = new SalesFixture();
        var customer = await fixture.NewCustomerAsync();
        var set = new SetCustomerPhoto.Handler(fixture.DbFactory);
        var get = new GetCustomerPhoto.Handler(fixture.DbFactory);
        await set.HandleAsync(new SetCustomerPhoto.Command(customer.Id, Png), Ct);

        var result = await fixture.UpdateCustomer.HandleAsync(
            new UpdateCustomer.Command(customer.Id, SalesFixture.Customer("Acme School Ltd")), Ct);

        result.IsSuccess.ShouldBeTrue();
        (await get.HandleAsync(new GetCustomerPhoto.Query(customer.Id), Ct)).Value.ShouldBe(Png);
    }

    [Fact]
    public async Task DeletingACustomerPublishesCustomerDeleted()
    {
        using var fixture = new SalesFixture();
        var customer = await fixture.NewCustomerAsync();

        (await fixture.DeleteCustomer.HandleAsync(new DeleteCustomer.Command(customer.Id), Ct)).IsSuccess.ShouldBeTrue();

        fixture.Events.Published.ShouldContain(new CustomerDeleted(customer.Id));
    }

    [Fact]
    public async Task ARefusedDeletePublishesNothing()
    {
        using var fixture = new SalesFixture();

        (await fixture.DeleteCustomer.HandleAsync(new DeleteCustomer.Command(Guid.NewGuid()), Ct)).IsSuccess.ShouldBeFalse();

        fixture.Events.Published.ShouldBeEmpty();
    }
}
