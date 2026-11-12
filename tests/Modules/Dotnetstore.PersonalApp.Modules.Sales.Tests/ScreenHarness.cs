using Dotnetstore.PersonalApp.Modules.Sales.Domain;
using Dotnetstore.PersonalApp.Modules.Sales.Features.Invoices;
using Dotnetstore.PersonalApp.Modules.Sales.Features.Offers;
using Dotnetstore.PersonalApp.Modules.Sales.Features.Orders;
using Dotnetstore.PersonalApp.Modules.Sales.Features.Pdf;
using Dotnetstore.PersonalApp.Modules.Sales.Features.PickLists;
using Dotnetstore.PersonalApp.Modules.Sales.Resources;
using Dotnetstore.PersonalApp.Shared.Files;
using Dotnetstore.PersonalApp.Shared.Products;
using Dotnetstore.PersonalApp.Shared.Localization;

namespace Dotnetstore.PersonalApp.Modules.Sales.Tests;

/// <summary>A real fixture with the pieces every Sales screen needs: English text, a save dialog that can be answered, and documents to open.</summary>
internal sealed class ScreenHarness : IDisposable
{
    public ScreenHarness()
    {
        Localizer.AddResources(SalesStrings.ResourceManager);
        Localizer.SetCulture("en");
        Pdf = new SalesPdfSaver(
            new ExportSalesPdf.Handler(Fixture.GetOffer, Fixture.GetOrder, Fixture.GetPickList, Fixture.GetInvoice, Localizer),
            FileSave,
            Localizer);
    }

    public SalesFixture Fixture { get; } = new();

    public Localizer Localizer { get; } = new();

    public RecordingFileSaver FileSave { get; } = new();

    public SalesPdfSaver Pdf { get; }

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public void Dispose() => Fixture.Dispose();

    public Task<OfferDetailDto> AcceptedOfferAsync(params OfferLineFields[] lines) => AcceptedOfferForAsync(null, lines);

    public async Task<OfferDetailDto> AcceptedOfferForAsync(Guid? customerId, params OfferLineFields[] lines)
    {
        var customer = customerId ?? (await Fixture.NewCustomerAsync(Guid.NewGuid().ToString("N"))).Id;
        var offer = await Fixture.NewOfferAsync(customer, lines);
        await Fixture.ChangeStatus.HandleAsync(new ChangeOfferStatus.Command(offer.Offer.Id, OfferStatus.Sent), Ct);
        await Fixture.ChangeStatus.HandleAsync(new ChangeOfferStatus.Command(offer.Offer.Id, OfferStatus.Accepted), Ct);
        return offer;
    }

    /// <summary>An order of one consulting line (nothing to pick, so it is ready to invoice): 4 hours at 100 + 25 % VAT = 500.</summary>
    public async Task<OrderDetailDto> ServiceOrderAsync(Guid? customerId = null)
    {
        var offer = await AcceptedOfferForAsync(customerId, SalesFixture.Line("Consulting", quantity: 4, price: 100));
        return (await Fixture.CreateOrder.HandleAsync(new CreateOrderFromOffer.Command(offer.Offer.Id), Ct)).Value;
    }

    /// <summary>An order of goods with a pick list; nothing is picked yet.</summary>
    public async Task<OrderDetailDto> GoodsOrderAsync(decimal quantity = 10, decimal inStock = 100)
    {
        var productId = Guid.NewGuid();
        Fixture.Stock.Add(productId, Guid.NewGuid(), inStock);
        var offer = await AcceptedOfferAsync(new OfferLineFields(productId, "Cable", quantity, "Piece", 100, 25, 0, IsPhysical: true));
        return (await Fixture.CreateOrder.HandleAsync(new CreateOrderFromOffer.Command(offer.Offer.Id), Ct)).Value;
    }

    public async Task<Guid> PickListIdAsync(Guid orderId) =>
        (await Fixture.ListPickLists.HandleAsync(new ListPickLists.Query(), Ct)).Items.Single(p => p.OrderId == orderId).Id;

    /// <summary>An unpaid invoice of 500 (see <see cref="ServiceOrderAsync"/>).</summary>
    public async Task<InvoiceDetailDto> InvoiceAsync(Guid? customerId = null)
    {
        var order = await ServiceOrderAsync(customerId);
        return (await Fixture.CreateInvoice.HandleAsync(new CreateInvoiceFromOrder.Command(order.Order.Id), Ct)).Value;
    }

    public OrderScreenViewModel OrderScreen(Guid orderId) =>
        new(orderId, Fixture.GetOrder, Fixture.CancelOrder, Fixture.CreateInvoice, Localizer, Pdf);

    public PickListScreenViewModel PickListScreen(Guid pickListId) =>
        new(pickListId, Fixture.GetPickList, Fixture.SavePicked, Localizer, Pdf);

    public InvoiceScreenViewModel InvoiceScreen(Guid invoiceId) =>
        new(invoiceId, Fixture.GetInvoice, Fixture.RecordPayment, Fixture.ApplyCredit, Fixture.DeletePayment, Fixture.CancelInvoice, Localizer, Pdf);

    public StubProductCatalog Catalog { get; } = new();

    public OfferScreenViewModel OfferScreen(Guid? offerId = null) =>
        new(
            offerId,
            Fixture.CreateOffer,
            Fixture.UpdateOffer,
            Fixture.GetOffer,
            Fixture.ChangeStatus,
            Fixture.DeleteOffer,
            Fixture.CreateOrder,
            Fixture.ListCustomers,
            Catalog,
            Localizer,
            Fixture.Clock,
            Pdf);

    internal sealed class StubProductCatalog : IProductCatalog
    {
        public List<ProductSummary> Products { get; } = [];

        public string? LastSearch { get; private set; }

        public Task<IReadOnlyList<ProductSummary>> SearchAsync(string? search, int maxCount, CancellationToken cancellationToken)
        {
            LastSearch = search;
            return Task.FromResult<IReadOnlyList<ProductSummary>>(
                [.. Products.Where(p => string.IsNullOrWhiteSpace(search) || p.Name.Contains(search, StringComparison.OrdinalIgnoreCase)).Take(maxCount)]);
        }
    }

    internal sealed class RecordingFileSaver : IFileSaveService
    {
        /// <summary>What the user picks in the dialog; null means they cancelled.</summary>
        public string? PickedPath { get; set; } = @"C:\temp\document.pdf";

        public string? SuggestedFileName { get; private set; }

        public byte[]? Content { get; private set; }

        public Task<string?> SaveAsync(string title, string suggestedFileName, byte[] content, CancellationToken cancellationToken)
        {
            SuggestedFileName = suggestedFileName;
            Content = content;
            return Task.FromResult(PickedPath);
        }
    }
}
