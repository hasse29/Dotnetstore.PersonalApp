using Dotnetstore.PersonalApp.Modules.Sales.Features;
using Dotnetstore.PersonalApp.Modules.Sales.Features.Customers;
using Dotnetstore.PersonalApp.Modules.Sales.Features.SalesOrderDetails;
using Dotnetstore.PersonalApp.Modules.Sales.Features.SalesOrders;
using Microsoft.Extensions.DependencyInjection;

namespace Dotnetstore.PersonalApp.Modules.Sales.Tests;

public sealed class OrderScreenTests : IDisposable
{
    private readonly SalesFixture _fixture = new();

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public void Dispose() => _fixture.Dispose();

    [Fact]
    public async Task LinesCanBeListedForOneOrderOnly()
    {
        var first = await _fixture.MakeSalesOrderDetailAsync(Ct);
        await _fixture.MakeSalesOrderDetailAsync(Ct);

        var lines = await _fixture.Services.GetRequiredService<ListSalesOrderDetails.Handler>()
            .HandleAsync(new ListSalesOrderDetails.Query(ParentId: first.SalesOrderId), Ct);

        lines.Items.ShouldHaveSingleItem().Id.ShouldBe(first.Id);
    }

    [Fact]
    public async Task ANewLineOnAnEmbeddedPageBelongsToTheParentWhoseEditorHidesTheChoice()
    {
        var order = await _fixture.MakeSalesOrderAsync(Ct);
        var page = _fixture.Services.GetRequiredService<SalesOrderDetailsViewModel>().ForParent(order.Id);

        await page.LoadCommand.ExecuteAsync(null);
        page.NewCommand.Execute(null);

        page.IsEmbedded.ShouldBeTrue();
        page.Editor.ShouldNotBeNull().IsParentFixed.ShouldBeTrue();
        page.Editor.SelectedSalesOrder.ShouldNotBeNull().Id.ShouldBe(order.Id);
    }

    [Fact]
    public async Task APageThatIsNotEmbeddedShowsItsOwnParentChoice()
    {
        var page = _fixture.Services.GetRequiredService<SalesOrderDetailsViewModel>();

        await page.LoadCommand.ExecuteAsync(null);
        page.NewCommand.Execute(null);

        page.IsEmbedded.ShouldBeFalse();
        page.Editor.ShouldNotBeNull().IsParentFixed.ShouldBeFalse();
    }

    [Fact]
    public async Task TheDetailPagesAreBuiltFromTheContainerAndHaveViews()
    {
        var customer = await _fixture.MakeCustomerAsync(Ct);
        var salesPerson = await _fixture.MakeSalesPersonAsync(Ct);
        var order = await _fixture.MakeSalesOrderAsync(Ct);

        var customerDetail = _fixture.Services.GetRequiredService<IDetailFactory<CustomerDto>>().Create(customer);
        var salesPersonDetail = _fixture.Services.GetRequiredService<IDetailFactory<Features.SalesPeople.SalesPersonDto>>().Create(salesPerson);
        var screen = _fixture.Services.GetRequiredService<IDetailFactory<SalesOrderDto>>().Create(order);

        customerDetail.ShouldBeOfType<CustomerDetailViewModel>().SalesOrders.IsEmbedded.ShouldBeTrue();
        salesPersonDetail.ShouldBeOfType<Features.SalesPeople.SalesPersonDetailViewModel>().QuotaHistory.IsEmbedded.ShouldBeTrue();
        screen.ShouldBeOfType<SalesOrderScreenViewModel>().Lines.IsEmbedded.ShouldBeTrue();
        foreach (var viewModel in new DetailViewModel[] { customerDetail, salesPersonDetail, screen })
        {
            var viewName = viewModel.GetType().FullName!.Replace("ViewModel", "View", StringComparison.Ordinal);
            viewModel.GetType().Assembly.GetType(viewName).ShouldNotBeNull();
        }
    }

    [Fact]
    public async Task TheScreenTotalsFollowTheLinesAfterOneIsDeleted()
    {
        var order = await _fixture.MakeSalesOrderAsync(Ct);
        var command = await _fixture.SalesOrderDetailCommandAsync(Ct);
        var create = _fixture.Services.GetRequiredService<CreateSalesOrderDetail.Handler>();
        await create.HandleAsync(command with { SalesOrderId = order.Id, OrderQty = 1, UnitPrice = 10m, UnitPriceDiscount = 0m }, Ct);
        await create.HandleAsync(command with { SalesOrderId = order.Id, OrderQty = 1, UnitPrice = 30m, UnitPriceDiscount = 0m }, Ct);
        var current = (await _fixture.Services.GetRequiredService<ListSalesOrders.Handler>().HandleAsync(new ListSalesOrders.Query(), Ct)).Items.Single(o => o.Id == order.Id);
        var screen = (SalesOrderScreenViewModel)_fixture.Services.GetRequiredService<IDetailFactory<SalesOrderDto>>().Create(current);
        await screen.LoadCommand.ExecuteAsync(null);

        screen.Order.SubTotal.ShouldBe(40m);
        screen.Lines.SelectedSalesOrderDetail = screen.Lines.SalesOrderDetails.Single(l => l.UnitPrice == 30m);
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
