using Dotnetstore.PersonalApp.Modules.Purchasing.Features;
using Dotnetstore.PersonalApp.Modules.Purchasing.Features.PurchaseOrderDetails;
using Dotnetstore.PersonalApp.Modules.Purchasing.Features.PurchaseOrders;
using Dotnetstore.PersonalApp.Modules.Purchasing.Features.Vendors;
using Microsoft.Extensions.DependencyInjection;

namespace Dotnetstore.PersonalApp.Modules.Purchasing.Tests;

public sealed class OrderScreenTests : IDisposable
{
    private readonly PurchasingFixture _fixture = new();

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public void Dispose() => _fixture.Dispose();

    [Fact]
    public async Task LinesCanBeListedForOneOrderOnly()
    {
        var first = await _fixture.MakePurchaseOrderDetailAsync(Ct);
        await _fixture.MakePurchaseOrderDetailAsync(Ct);

        var lines = await _fixture.Services.GetRequiredService<ListPurchaseOrderDetails.Handler>()
            .HandleAsync(new ListPurchaseOrderDetails.Query(ParentId: first.PurchaseOrderId), Ct);

        lines.Items.ShouldHaveSingleItem().Id.ShouldBe(first.Id);
    }

    [Fact]
    public async Task ANewLineOnAnEmbeddedPageBelongsToTheParentWhoseEditorHidesTheChoice()
    {
        var order = await _fixture.MakePurchaseOrderAsync(Ct);
        var page = _fixture.Services.GetRequiredService<PurchaseOrderDetailsViewModel>().ForParent(order.Id);

        await page.LoadCommand.ExecuteAsync(null);
        page.NewCommand.Execute(null);

        page.IsEmbedded.ShouldBeTrue();
        page.Editor.ShouldNotBeNull().IsParentFixed.ShouldBeTrue();
        page.Editor.SelectedPurchaseOrder.ShouldNotBeNull().Id.ShouldBe(order.Id);
    }

    [Fact]
    public async Task TheDetailPagesAreBuiltFromTheContainerAndHaveViews()
    {
        var vendor = await _fixture.MakeVendorAsync(Ct);
        var order = await _fixture.MakePurchaseOrderAsync(Ct);

        var vendorDetail = _fixture.Services.GetRequiredService<IDetailFactory<VendorDto>>().Create(vendor);
        var screen = _fixture.Services.GetRequiredService<IDetailFactory<PurchaseOrderDto>>().Create(order);

        vendorDetail.ShouldBeOfType<VendorDetailViewModel>().PurchaseOrders.IsEmbedded.ShouldBeTrue();
        screen.ShouldBeOfType<PurchaseOrderScreenViewModel>().Lines.IsEmbedded.ShouldBeTrue();
        foreach (var viewModel in new DetailViewModel[] { vendorDetail, screen })
        {
            var viewName = viewModel.GetType().FullName!.Replace("ViewModel", "View", StringComparison.Ordinal);
            viewModel.GetType().Assembly.GetType(viewName).ShouldNotBeNull();
        }
    }

    [Fact]
    public async Task TheScreenTotalsFollowTheLinesAfterOneIsDeleted()
    {
        var order = await _fixture.MakePurchaseOrderAsync(Ct);
        var command = await _fixture.PurchaseOrderDetailCommandAsync(Ct);
        var create = _fixture.Services.GetRequiredService<CreatePurchaseOrderDetail.Handler>();
        await create.HandleAsync(command with { PurchaseOrderId = order.Id, OrderQty = 1, UnitPrice = 10m, ReceivedQty = 0m, RejectedQty = 0m }, Ct);
        await create.HandleAsync(command with { PurchaseOrderId = order.Id, OrderQty = 1, UnitPrice = 30m, ReceivedQty = 0m, RejectedQty = 0m }, Ct);
        var current = (await _fixture.Services.GetRequiredService<ListPurchaseOrders.Handler>().HandleAsync(new ListPurchaseOrders.Query(), Ct)).Items.Single(o => o.Id == order.Id);
        var screen = (PurchaseOrderScreenViewModel)_fixture.Services.GetRequiredService<IDetailFactory<PurchaseOrderDto>>().Create(current);
        await screen.LoadCommand.ExecuteAsync(null);

        screen.Order.SubTotal.ShouldBe(40m);
        screen.Lines.SelectedPurchaseOrderDetail = screen.Lines.PurchaseOrderDetails.Single(l => l.UnitPrice == 30m);
        screen.Lines.RequestDeleteCommand.Execute(null);
        await screen.Lines.ConfirmDeleteCommand.ExecuteAsync(null);

        // The refresh runs in an event handler, so give it a moment to finish.
        for (var i = 0; i < 100 && screen.Order.SubTotal != 10m; i++)
        {
            await Task.Delay(20, Ct);
        }

        screen.Order.SubTotal.ShouldBe(10m);
        screen.Order.TotalDue.ShouldBe(10m + order.TaxAmt + order.Freight);
    }
}
