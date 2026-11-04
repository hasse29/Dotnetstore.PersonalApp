using Dotnetstore.PersonalApp.Modules.Sales.Consumers;
using Dotnetstore.PersonalApp.Modules.Sales.Persistence;
using Dotnetstore.PersonalApp.Shared.Contracts.Events;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Dotnetstore.PersonalApp.Modules.Sales.Tests;

public sealed class GoodsIssuedConsumerTests : IDisposable
{
    private readonly SalesFixture _fixture = new();

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public void Dispose() => _fixture.Dispose();

    private GoodsIssuedConsumer Consumer() => new(
        _fixture.Services.GetRequiredService<IDbContextFactory<SalesDbContext>>(),
        _fixture.Services.GetRequiredService<TimeProvider>());

    private async Task<(int Shipped, string Status, DateOnly? ShipDate)> ReadAsync(Guid lineId)
    {
        await using var db = await _fixture.Services.GetRequiredService<IDbContextFactory<SalesDbContext>>().CreateDbContextAsync(Ct);
        var line = await db.SalesOrderDetails.Include(x => x.SalesOrder).SingleAsync(x => x.Id == lineId, Ct);
        return (line.ShippedQty, line.SalesOrder.Status, line.SalesOrder.ShipDate);
    }

    [Fact]
    public async Task APartialShipmentRecordsTheQuantityAndLeavesTheOrderOpen()
    {
        var line = await _fixture.MakeSalesOrderDetailAsync(Ct);

        var before = await ReadAsync(line.Id);

        await Consumer().ConsumeAsync(new GoodsIssued(line.Id, line.OrderQty - 1), Ct);

        var after = await ReadAsync(line.Id);
        after.Shipped.ShouldBe(line.OrderQty - 1);
        after.Status.ShouldBe(before.Status);
        after.ShipDate.ShouldBe(before.ShipDate);
    }

    [Fact]
    public async Task ShippingEveryLineMarksTheOrderShippedAndSendingTheTotalAgainChangesNothing()
    {
        var line = await _fixture.MakeSalesOrderDetailAsync(Ct);

        await Consumer().ConsumeAsync(new GoodsIssued(line.Id, line.OrderQty), Ct);
        await Consumer().ConsumeAsync(new GoodsIssued(line.Id, line.OrderQty), Ct);

        var (shipped, status, shipDate) = await ReadAsync(line.Id);
        shipped.ShouldBe(line.OrderQty);
        status.ShouldBe("S");
        shipDate.ShouldNotBeNull();
    }
}
