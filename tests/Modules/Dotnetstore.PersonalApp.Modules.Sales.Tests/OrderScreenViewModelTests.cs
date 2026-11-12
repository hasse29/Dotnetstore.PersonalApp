using System.Globalization;
using Dotnetstore.PersonalApp.Modules.Sales.Features.Invoices;

namespace Dotnetstore.PersonalApp.Modules.Sales.Tests;

public sealed class OrderScreenViewModelTests : IDisposable
{
    private readonly ScreenHarness _harness = new();

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public void Dispose() => _harness.Dispose();

    private static string Money(decimal amount) => amount.ToString("N2", CultureInfo.CurrentCulture);

    [Fact]
    public async Task LoadShowsTheOrderItsLinesAndItsTotals()
    {
        var order = await _harness.ServiceOrderAsync();
        var screen = _harness.OrderScreen(order.Order.Id);

        await screen.LoadCommand.ExecuteAsync(null);

        screen.Heading.ShouldBe(order.Order.Number);
        screen.StatusText.ShouldBe(_harness.Localizer.Get("OrderStatus_ReadyToInvoice"));
        screen.Lines.Single().AmountText.ShouldBe(Money(400));
        screen.NetText.ShouldBe(Money(400));
        screen.VatText.ShouldBe(Money(100));
        screen.TotalText.ShouldBe(Money(500));
        screen.CustomerText.ShouldContain(order.Order.CustomerName);
        screen.OfferText.ShouldBe(_harness.Localizer.Format("OrderScreen_FromOffer", (object)order.Order.OfferNumber));
        screen.PickText.ShouldBe(_harness.Localizer.Get("OrderScreen_NothingToPick"));
    }

    [Fact]
    public async Task AnOrderWithGoodsShowsHowFarThePickingHasCome()
    {
        var order = await _harness.GoodsOrderAsync();
        var screen = _harness.OrderScreen(order.Order.Id);

        await screen.LoadCommand.ExecuteAsync(null);

        screen.PickText.ShouldBe(_harness.Localizer.Format("OrderScreen_PickList", order.PickListNumber!, 0, 1));
        screen.CanInvoice.ShouldBeFalse();
        screen.CanCancel.ShouldBeTrue();
    }

    [Fact]
    public async Task AnUnknownOrderShowsTheErrorAndNothingElse()
    {
        var screen = _harness.OrderScreen(Guid.NewGuid());

        await screen.LoadCommand.ExecuteAsync(null);

        screen.StatusMessage.ShouldBe(_harness.Localizer.Get("Error_order_not_found"));
        screen.Heading.ShouldBeEmpty();
        screen.Lines.ShouldBeEmpty();
        screen.CanCancel.ShouldBeFalse();
        screen.CanInvoice.ShouldBeFalse();
    }

    [Fact]
    public async Task CancellingNeedsAConfirmationThatCanBeWithdrawn()
    {
        var order = await _harness.ServiceOrderAsync();
        var screen = _harness.OrderScreen(order.Order.Id);
        await screen.LoadCommand.ExecuteAsync(null);

        screen.RequestCancelCommand.Execute(null);
        screen.IsConfirmingCancel.ShouldBeTrue();

        screen.CancelCancelCommand.Execute(null);
        screen.IsConfirmingCancel.ShouldBeFalse();
        screen.CanCancel.ShouldBeTrue();
    }

    [Fact]
    public async Task ConfirmingTheCancellationCancelsTheOrder()
    {
        var order = await _harness.GoodsOrderAsync();
        var screen = _harness.OrderScreen(order.Order.Id);
        await screen.LoadCommand.ExecuteAsync(null);
        screen.RequestCancelCommand.Execute(null);

        await screen.ConfirmCancelCommand.ExecuteAsync(null);

        screen.IsConfirmingCancel.ShouldBeFalse();
        screen.StatusMessage.ShouldBe(_harness.Localizer.Get("OrderScreen_Cancelled"));
        screen.StatusText.ShouldBe(_harness.Localizer.Get("OrderStatus_Cancelled"));
        screen.CanCancel.ShouldBeFalse();
        screen.CanInvoice.ShouldBeFalse();
    }

    [Fact]
    public async Task AnInvoicedOrderCannotBeCancelledAndTheScreenSaysWhy()
    {
        var order = await _harness.ServiceOrderAsync();
        var screen = _harness.OrderScreen(order.Order.Id);
        await screen.LoadCommand.ExecuteAsync(null);
        await screen.CreateInvoiceCommand.ExecuteAsync(null);

        screen.CanCancel.ShouldBeFalse();
        await screen.ConfirmCancelCommand.ExecuteAsync(null);

        screen.StatusMessage.ShouldBe(_harness.Localizer.Get("Error_order_invoiced"));
        screen.StatusText.ShouldBe(_harness.Localizer.Get("OrderStatus_Invoiced"));
    }

    [Fact]
    public async Task CreatingTheInvoiceReportsItsNumberAndHidesTheButton()
    {
        var order = await _harness.ServiceOrderAsync();
        var screen = _harness.OrderScreen(order.Order.Id);
        await screen.LoadCommand.ExecuteAsync(null);
        screen.CanInvoice.ShouldBeTrue();

        await screen.CreateInvoiceCommand.ExecuteAsync(null);

        var invoices = await _harness.Fixture.ListInvoices.HandleAsync(new ListInvoices.Query(), Ct);
        var number = invoices.Items.Single().Number;
        screen.StatusMessage.ShouldBe(_harness.Localizer.Format("OrderScreen_InvoiceCreated", (object)number));
        screen.InvoiceText.ShouldBe(_harness.Localizer.Format("OrderScreen_HasInvoice", (object)number));
        screen.CanInvoice.ShouldBeFalse();
    }

    [Fact]
    public async Task TheInvoiceUsesTheChosenPaymentTermsAndFallsBackToTheDefaultWhenBlank()
    {
        var first = await _harness.ServiceOrderAsync();
        var second = await _harness.ServiceOrderAsync();
        var withTerms = _harness.OrderScreen(first.Order.Id);
        var blank = _harness.OrderScreen(second.Order.Id);
        withTerms.PaymentDays.ShouldBe(CreateInvoiceFromOrder.DefaultPaymentDays);
        withTerms.PaymentDays = 10;
        blank.PaymentDays = null;

        await withTerms.CreateInvoiceCommand.ExecuteAsync(null);
        await blank.CreateInvoiceCommand.ExecuteAsync(null);

        var invoices = (await _harness.Fixture.ListInvoices.HandleAsync(new ListInvoices.Query(), Ct)).Items;
        var days = invoices.Select(i => i.DueDate.DayNumber - i.Date.DayNumber).Order().ToList();
        days.ShouldBe([10, CreateInvoiceFromOrder.DefaultPaymentDays]);
    }

    [Fact]
    public async Task InvalidPaymentTermsAreRejectedWithoutCreatingAnInvoice()
    {
        var order = await _harness.ServiceOrderAsync();
        var screen = _harness.OrderScreen(order.Order.Id);
        await screen.LoadCommand.ExecuteAsync(null);
        screen.PaymentDays = 400;

        await screen.CreateInvoiceCommand.ExecuteAsync(null);

        screen.StatusMessage.ShouldBe(_harness.Localizer.Get("Error_invoice_payment_days_invalid"));
        screen.CanInvoice.ShouldBeTrue();
        (await _harness.Fixture.ListInvoices.HandleAsync(new ListInvoices.Query(), Ct)).TotalCount.ShouldBe(0);
    }

    [Fact]
    public async Task AnOrderStillBeingPickedCannotBeInvoiced()
    {
        var order = await _harness.GoodsOrderAsync();
        var screen = _harness.OrderScreen(order.Order.Id);
        await screen.LoadCommand.ExecuteAsync(null);

        await screen.CreateInvoiceCommand.ExecuteAsync(null);

        screen.StatusMessage.ShouldBe(_harness.Localizer.Get("Error_invoice_order_not_ready"));
        screen.InvoiceText.ShouldBeEmpty();
    }

    [Fact]
    public async Task BackTellsTheHostToReturnToTheList()
    {
        var screen = _harness.OrderScreen(Guid.NewGuid());
        bool? refresh = null;
        screen.Closed += (_, value) => refresh = value;

        screen.BackCommand.Execute(null);

        refresh.ShouldBe(true);
    }

    [Fact]
    public async Task ExportPdfSavesThePdfAndTellsWhereItWent()
    {
        var order = await _harness.ServiceOrderAsync();
        var screen = _harness.OrderScreen(order.Order.Id);

        await screen.ExportPdfCommand.ExecuteAsync(null);

        _harness.FileSave.Content.ShouldNotBeNull().Length.ShouldBeGreaterThan(100);
        _harness.FileSave.SuggestedFileName.ShouldNotBeNull().ShouldEndWith(".pdf");
        screen.StatusMessage.ShouldBe(_harness.Localizer.Format("SalesPdf_Saved", @"C:\temp\document.pdf"));
    }

    [Fact]
    public async Task CancellingTheSaveDialogLeavesTheStatusAlone()
    {
        var order = await _harness.ServiceOrderAsync();
        var screen = _harness.OrderScreen(order.Order.Id);
        _harness.FileSave.PickedPath = null;

        await screen.ExportPdfCommand.ExecuteAsync(null);

        screen.StatusMessage.ShouldBeNull();
    }

    [Fact]
    public async Task ExportingAnUnknownOrderShowsTheError()
    {
        var screen = _harness.OrderScreen(Guid.NewGuid());

        await screen.ExportPdfCommand.ExecuteAsync(null);

        screen.StatusMessage.ShouldBe(_harness.Localizer.Get("Error_order_not_found"));
        _harness.FileSave.Content.ShouldBeNull();
    }
}
