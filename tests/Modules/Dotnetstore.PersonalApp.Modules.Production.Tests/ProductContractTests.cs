using Dotnetstore.PersonalApp.Modules.Production.Features.Products;
using Dotnetstore.PersonalApp.Shared.Contracts.Events;
using Microsoft.Extensions.DependencyInjection;

namespace Dotnetstore.PersonalApp.Modules.Production.Tests;

public sealed class ProductContractTests : IDisposable
{
    private readonly ProductionFixture _fixture = new();

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public void Dispose() => _fixture.Dispose();

    [Fact]
    public async Task DeletingAProductPublishesProductDeleted()
    {
        var product = await _fixture.MakeProductAsync(Ct);

        await _fixture.Services.GetRequiredService<DeleteProduct.Handler>().HandleAsync(new DeleteProduct.Command(product.Id), Ct);

        _fixture.Services.GetRequiredService<RecordingEventPublisher>().Published.ShouldContain(new ProductDeleted(product.Id));
    }

    [Fact]
    public async Task AProductThatIsNotFoundPublishesNothing()
    {
        await _fixture.Services.GetRequiredService<DeleteProduct.Handler>().HandleAsync(new DeleteProduct.Command(Guid.NewGuid()), Ct);

        _fixture.Services.GetRequiredService<RecordingEventPublisher>().Published.ShouldBeEmpty();
    }

    [Fact]
    public async Task DatesMustNotEndBeforeTheyStart()
    {
        var command = await _fixture.ProductCommandAsync(Ct);

        var result = await _fixture.Services.GetRequiredService<CreateProduct.Handler>()
            .HandleAsync(command with { SellEndDate = command.SellStartDate.AddDays(-1) }, Ct);

        result.Error.Code.ShouldBe("product_end_before_start");
    }
}
