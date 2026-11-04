using Dotnetstore.PersonalApp.Modules.Inventory.Features.Receiving;
using Dotnetstore.PersonalApp.Modules.Inventory.Features.StockMovements;
using Dotnetstore.PersonalApp.Modules.Inventory.Features.StorageLocations;
using Dotnetstore.PersonalApp.Shared.Contracts.Events;
using Microsoft.Extensions.DependencyInjection;

namespace Dotnetstore.PersonalApp.Modules.Inventory.Tests;

public sealed class GoodsEventTests : IDisposable
{
    private static readonly DateOnly Today = new(2026, 1, 1);

    private readonly InventoryFixture _fixture = new();

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public void Dispose() => _fixture.Dispose();

    private T Get<T>() where T : notnull => _fixture.Services.GetRequiredService<T>();

    private RecordingEventPublisher Events => Get<RecordingEventPublisher>();

    private async Task<Guid> LocationAsync()
    {
        var warehouse = await _fixture.MakeWarehouseAsync(Ct);
        return (await Get<CreateStorageLocation.Handler>().HandleAsync(new CreateStorageLocation.Command(warehouse.Id, "A1", null, null, null), Ct)).Value.Id;
    }

    [Fact]
    public async Task PartialReceiptsPublishARunningTotalPerLine()
    {
        var location = await LocationAsync();
        var product = Guid.NewGuid();
        var line = Guid.NewGuid();
        var handler = Get<OrderMovements.Handler>();

        await handler.RecordAsync(new OrderMovements.Command(true, location, "PO-1", Today, [new OrderMovements.Line(line, product, 10m, 4m)]), Ct);
        await handler.RecordAsync(new OrderMovements.Command(true, location, "PO-1", Today, [new OrderMovements.Line(line, product, 10m, 3m)]), Ct);

        Events.Published.OfType<GoodsReceived>().Select(x => x.TotalQuantity).ShouldBe([4m, 7m]);
    }

    [Fact]
    public async Task TwoLinesForTheSameProductAreCappedSeparately()
    {
        var location = await LocationAsync();
        var product = Guid.NewGuid();
        var first = Guid.NewGuid();
        var second = Guid.NewGuid();
        var handler = Get<OrderMovements.Handler>();

        var ok = await handler.RecordAsync(new OrderMovements.Command(true, location, "PO-1", Today,
            [new OrderMovements.Line(first, product, 5m, 5m), new OrderMovements.Line(second, product, 5m, 0m)]), Ct);
        var tooMuch = await handler.RecordAsync(new OrderMovements.Command(true, location, "PO-1", Today,
            [new OrderMovements.Line(first, product, 5m, 1m), new OrderMovements.Line(second, product, 5m, 0m)]), Ct);

        ok.IsSuccess.ShouldBeTrue();
        tooMuch.Error.Code.ShouldBe("goods_movement_exceeds_order");
    }

    [Fact]
    public async Task IssuingPublishesGoodsIssuedWithTheTotal()
    {
        var location = await LocationAsync();
        var product = Guid.NewGuid();
        var line = Guid.NewGuid();
        await Get<CreateStockMovement.Handler>().HandleAsync(new CreateStockMovement.Command(Today, product, location, "Receipt", 20m, null, null), Ct);

        await Get<OrderMovements.Handler>().RecordAsync(new OrderMovements.Command(false, location, "SO-1", Today, [new OrderMovements.Line(line, product, 6m, 2m)]), Ct);
        await Get<OrderMovements.Handler>().RecordAsync(new OrderMovements.Command(false, location, "SO-1", Today, [new OrderMovements.Line(line, product, 6m, 4m)]), Ct);

        Events.Published.OfType<GoodsIssued>().Select(x => x.TotalQuantity).ShouldBe([2m, 6m]);
    }

    [Fact]
    public async Task OpeningAnOrderAgainResendsTheTotalsSoAMissedMessageIsMadeGood()
    {
        var location = await LocationAsync();
        var product = Guid.NewGuid();
        var line = Guid.NewGuid();
        var lines = new[] { new OrderMovements.Line(line, product, 10m, 0m) };
        await Get<OrderMovements.Handler>().RecordAsync(new OrderMovements.Command(true, location, "PO-1", Today, [new OrderMovements.Line(line, product, 10m, 6m)]), Ct);
        Events.Published.Clear();

        await Get<OrderMovements.Handler>().ResyncAsync("PO-1", true, lines, Ct);

        Events.Published.OfType<GoodsReceived>().ShouldHaveSingleItem().TotalQuantity.ShouldBe(6m);
    }
}
