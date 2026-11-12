using System.Text;
using Dotnetstore.PersonalApp.Modules.Sales.Domain;
using Dotnetstore.PersonalApp.Modules.Sales.Features.Invoices;
using Dotnetstore.PersonalApp.Modules.Sales.Features.Offers;
using Dotnetstore.PersonalApp.Modules.Sales.Features.Orders;
using Dotnetstore.PersonalApp.Modules.Sales.Features.Pdf;
using Dotnetstore.PersonalApp.Modules.Sales.Features.PickLists;
using Dotnetstore.PersonalApp.Modules.Sales.Resources;
using Dotnetstore.PersonalApp.Shared.Localization;

namespace Dotnetstore.PersonalApp.Modules.Sales.Tests;

public sealed class SalesPdfTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private static ExportSalesPdf.Handler Export(SalesFixture fixture)
    {
        var localizer = new Localizer();
        localizer.AddResources(SalesStrings.ResourceManager);
        return new ExportSalesPdf.Handler(fixture.GetOffer, fixture.GetOrder, fixture.GetPickList, fixture.GetInvoice, localizer);
    }

    [Fact]
    public async Task EveryDocumentOfASaleCanBeExportedToPdf()
    {
        using var fixture = new SalesFixture();
        var customer = await fixture.NewCustomerAsync();
        var offer = await fixture.NewOfferAsync(customer.Id, SalesFixture.Line("Cat6 cable 2m", 10, 50, 25, 10), SalesFixture.Line("Installation 安装", 1, 900));
        await fixture.ChangeStatus.HandleAsync(new ChangeOfferStatus.Command(offer.Offer.Id, OfferStatus.Sent), Ct);
        await fixture.ChangeStatus.HandleAsync(new ChangeOfferStatus.Command(offer.Offer.Id, OfferStatus.Accepted), Ct);
        var order = (await fixture.CreateOrder.HandleAsync(new CreateOrderFromOffer.Command(offer.Offer.Id), Ct)).Value;
        var invoice = (await fixture.CreateInvoice.HandleAsync(new CreateInvoiceFromOrder.Command(order.Order.Id), Ct)).Value;
        await fixture.RecordPayment.HandleAsync(new RecordPayment.Command(invoice.Invoice.Id, 100m, null, "Part payment"), Ct);
        var physical = await fixture.NewOfferAsync(customer.Id, new OfferLineFields(Guid.NewGuid(), "Switch 8-port", 2, "Piece", 300, 25, 0, IsPhysical: true));
        await fixture.ChangeStatus.HandleAsync(new ChangeOfferStatus.Command(physical.Offer.Id, OfferStatus.Sent), Ct);
        await fixture.ChangeStatus.HandleAsync(new ChangeOfferStatus.Command(physical.Offer.Id, OfferStatus.Accepted), Ct);
        var physicalOrder = (await fixture.CreateOrder.HandleAsync(new CreateOrderFromOffer.Command(physical.Offer.Id), Ct)).Value;
        var pickList = (await fixture.ListPickLists.HandleAsync(new ListPickLists.Query(), Ct)).Items.Single(p => p.OrderId == physicalOrder.Order.Id);

        var export = Export(fixture);
        List<ExportSalesPdf.Query> queries =
        [
            new(SalesDocumentKind.Offer, offer.Offer.Id),
            new(SalesDocumentKind.Order, order.Order.Id),
            new(SalesDocumentKind.Invoice, invoice.Invoice.Id),
            new(SalesDocumentKind.PickList, pickList.Id),
        ];

        foreach (var query in queries)
        {
            var result = await export.HandleAsync(query, Ct);

            result.IsSuccess.ShouldBeTrue(query.Kind.ToString());
            Encoding.ASCII.GetString(result.Value.Content, 0, 5).ShouldBe("%PDF-");
            result.Value.FileName.ShouldEndWith(".pdf");
        }
    }

    [Fact]
    public async Task ADocumentThatDoesNotExistCannotBeExported()
    {
        using var fixture = new SalesFixture();

        var result = await Export(fixture).HandleAsync(new ExportSalesPdf.Query(SalesDocumentKind.Invoice, Guid.NewGuid()), Ct);

        result.IsSuccess.ShouldBeFalse();
        result.Error.Code.ShouldBe("invoice_not_found");
    }
}
