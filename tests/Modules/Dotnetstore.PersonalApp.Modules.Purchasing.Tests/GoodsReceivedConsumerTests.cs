using Dotnetstore.PersonalApp.Modules.Purchasing.Consumers;
using Dotnetstore.PersonalApp.Modules.Purchasing.Persistence;
using Dotnetstore.PersonalApp.Shared.Contracts.Events;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Dotnetstore.PersonalApp.Modules.Purchasing.Tests;

public sealed class GoodsReceivedConsumerTests : IDisposable
{
    private readonly PurchasingFixture _fixture = new();

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public void Dispose() => _fixture.Dispose();

    [Fact]
    public async Task TheTotalReplacesWhatWasThereSoDeliveringItTwiceDoesNoHarm()
    {
        var line = await _fixture.MakePurchaseOrderDetailAsync(Ct);
        var factory = _fixture.Services.GetRequiredService<IDbContextFactory<PurchasingDbContext>>();
        var consumer = new GoodsReceivedConsumer(factory);

        await consumer.ConsumeAsync(new GoodsReceived(line.Id, 4m), Ct);
        await consumer.ConsumeAsync(new GoodsReceived(line.Id, 4m), Ct);
        await consumer.ConsumeAsync(new GoodsReceived(line.Id, 6m), Ct);

        await using var db = await factory.CreateDbContextAsync(Ct);
        var saved = await db.PurchaseOrderDetails.SingleAsync(x => x.Id == line.Id, Ct);
        saved.ReceivedQty.ShouldBe(6m);
        saved.StockedQty.ShouldBe(6m - saved.RejectedQty);
    }
}
