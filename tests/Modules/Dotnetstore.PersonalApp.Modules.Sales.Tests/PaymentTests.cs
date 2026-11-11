using Dotnetstore.PersonalApp.Modules.Sales.Domain;
using Dotnetstore.PersonalApp.Modules.Sales.Features.Credits;
using Dotnetstore.PersonalApp.Modules.Sales.Features.Invoices;
using Dotnetstore.PersonalApp.Modules.Sales.Features.Offers;
using Dotnetstore.PersonalApp.Modules.Sales.Features.Orders;
using Dotnetstore.PersonalApp.Modules.Sales.Features.PickLists;
using Dotnetstore.PersonalApp.Shared.Results;

namespace Dotnetstore.PersonalApp.Modules.Sales.Tests;

/// <summary>Invoices of one consulting line, 4 hours at 100 + 25 % VAT = 500, for a customer of the test's choosing.</summary>
public sealed class PaymentTests
{
    private static async Task<InvoiceDetailDto> InvoiceAsync(SalesFixture fixture, Guid customerId)
    {
        var offer = await fixture.NewOfferAsync(customerId, SalesFixture.Line("Consulting", quantity: 4, price: 100));
        await fixture.ChangeStatus.HandleAsync(new ChangeOfferStatus.Command(offer.Offer.Id, OfferStatus.Sent), CancellationToken.None);
        await fixture.ChangeStatus.HandleAsync(new ChangeOfferStatus.Command(offer.Offer.Id, OfferStatus.Accepted), CancellationToken.None);
        var order = (await fixture.CreateOrder.HandleAsync(new CreateOrderFromOffer.Command(offer.Offer.Id), CancellationToken.None)).Value;
        return (await fixture.CreateInvoice.HandleAsync(new CreateInvoiceFromOrder.Command(order.Order.Id), CancellationToken.None)).Value;
    }

    private static async Task<(SalesFixture Fixture, Guid CustomerId, InvoiceDetailDto Invoice)> SetupAsync()
    {
        var fixture = new SalesFixture();
        var customer = await fixture.NewCustomerAsync();
        return (fixture, customer.Id, await InvoiceAsync(fixture, customer.Id));
    }

    private static Task<Result<RecordPayment.Response>> PayAsync(SalesFixture fixture, Guid invoiceId, decimal amount, DateOnly? date = null) =>
        fixture.RecordPayment.HandleAsync(new RecordPayment.Command(invoiceId, amount, date), CancellationToken.None);

    [Fact]
    public async Task SeveralPartialPaymentsAddUpToPaid()
    {
        var (fixture, _, invoice) = await SetupAsync();
        using var owner = fixture;

        var first = await PayAsync(fixture, invoice.Invoice.Id, 200);
        var second = await PayAsync(fixture, invoice.Invoice.Id, 100);
        var last = await PayAsync(fixture, invoice.Invoice.Id, 200);

        first.Value.Invoice.Invoice.Status.ShouldBe(InvoiceStatus.PartiallyPaid);
        first.Value.Invoice.Invoice.Outstanding.ShouldBe(300);
        second.Value.Invoice.Invoice.Outstanding.ShouldBe(200);
        last.Value.Invoice.Invoice.Status.ShouldBe(InvoiceStatus.Paid);
        last.Value.Invoice.Invoice.Outstanding.ShouldBe(0);
        last.Value.Invoice.Payments.Count.ShouldBe(3);
        last.Value.Excess.ShouldBe(0);
    }

    [Fact]
    public async Task OnePaymentInFullPaysTheInvoice()
    {
        var (fixture, _, invoice) = await SetupAsync();
        using var owner = fixture;

        var result = await PayAsync(fixture, invoice.Invoice.Id, 500, new DateOnly(2026, 11, 12));

        result.Value.Invoice.Invoice.Status.ShouldBe(InvoiceStatus.Paid);
        result.Value.Invoice.Payments.Single().Date.ShouldBe(new DateOnly(2026, 11, 12));
        result.Value.Invoice.CustomerCredit.ShouldBe(0);
    }

    [Fact]
    public async Task PayingTooMuchPaysTheInvoiceAndTheRestBecomesCredit()
    {
        var (fixture, customerId, invoice) = await SetupAsync();
        using var owner = fixture;
        await PayAsync(fixture, invoice.Invoice.Id, 300);

        var result = await PayAsync(fixture, invoice.Invoice.Id, 450);

        result.Value.Excess.ShouldBe(250);
        result.Value.Invoice.Invoice.Status.ShouldBe(InvoiceStatus.Paid);
        result.Value.Invoice.Invoice.Paid.ShouldBe(500);
        result.Value.Invoice.CustomerCredit.ShouldBe(250);
        var credits = await fixture.ListCredits.HandleAsync(CancellationToken.None);
        credits.Single().CustomerId.ShouldBe(customerId);
        credits.Single().Balance.ShouldBe(250);
    }

    [Fact]
    public async Task PayingAnAlreadyPaidInvoiceAgainIsAllCredit()
    {
        var (fixture, _, invoice) = await SetupAsync();
        using var owner = fixture;
        await PayAsync(fixture, invoice.Invoice.Id, 500);

        var result = await PayAsync(fixture, invoice.Invoice.Id, 100);

        result.Value.Excess.ShouldBe(100);
        result.Value.Invoice.Invoice.Paid.ShouldBe(500);
        result.Value.Invoice.CustomerCredit.ShouldBe(100);
    }

    [Fact]
    public async Task RejectsBadAmountsDatesAndCancelledInvoices()
    {
        var (fixture, _, invoice) = await SetupAsync();
        using var owner = fixture;
        var other = await InvoiceAsync(fixture, (await fixture.NewCustomerAsync("Other")).Id);
        await fixture.CancelInvoice.HandleAsync(new CancelInvoice.Command(other.Invoice.Id), CancellationToken.None);

        (await PayAsync(fixture, invoice.Invoice.Id, 0)).Error.Code.ShouldBe("payment_amount_invalid");
        (await PayAsync(fixture, invoice.Invoice.Id, -5)).Error.Code.ShouldBe("payment_amount_invalid");
        (await PayAsync(fixture, invoice.Invoice.Id, 10, new DateOnly(2026, 11, 9))).Error.Code.ShouldBe("payment_date_invalid");
        (await PayAsync(fixture, other.Invoice.Id, 10)).Error.Code.ShouldBe("invoice_cancelled");
        (await PayAsync(fixture, Guid.NewGuid(), 10)).Error.Code.ShouldBe("invoice_not_found");
    }

    [Fact]
    public async Task CreditCanPayAnotherInvoiceOfTheSameCustomer()
    {
        var (fixture, customerId, first) = await SetupAsync();
        using var owner = fixture;
        await PayAsync(fixture, first.Invoice.Id, 700);
        var second = await InvoiceAsync(fixture, customerId);

        var tooMuch = await fixture.ApplyCredit.HandleAsync(new ApplyCredit.Command(second.Invoice.Id, 300), CancellationToken.None);
        var applied = await fixture.ApplyCredit.HandleAsync(new ApplyCredit.Command(second.Invoice.Id, 200), CancellationToken.None);

        tooMuch.Error.Code.ShouldBe("credit_insufficient");
        applied.Value.Invoice.Paid.ShouldBe(200);
        applied.Value.Invoice.Status.ShouldBe(InvoiceStatus.PartiallyPaid);
        applied.Value.CustomerCredit.ShouldBe(0);
        applied.Value.Payments.Single().Kind.ShouldBe(PaymentKind.CreditApplied);
        (await fixture.ListCredits.HandleAsync(CancellationToken.None)).ShouldBeEmpty();
    }

    [Fact]
    public async Task CreditCannotPayMoreThanIsLeft()
    {
        var (fixture, customerId, first) = await SetupAsync();
        using var owner = fixture;
        await PayAsync(fixture, first.Invoice.Id, 500 + 900);
        var second = await InvoiceAsync(fixture, customerId);

        var result = await fixture.ApplyCredit.HandleAsync(new ApplyCredit.Command(second.Invoice.Id, 501), CancellationToken.None);

        result.Error.Code.ShouldBe("payment_exceeds_outstanding");
    }

    [Fact]
    public async Task CreditCanBePaidBackUpToTheBalance()
    {
        var (fixture, customerId, invoice) = await SetupAsync();
        using var owner = fixture;
        await PayAsync(fixture, invoice.Invoice.Id, 800);

        var tooMuch = await fixture.RefundCredit.HandleAsync(new RefundCredit.Command(customerId, 301), CancellationToken.None);
        var partly = await fixture.RefundCredit.HandleAsync(new RefundCredit.Command(customerId, 100, null, "bank"), CancellationToken.None);

        tooMuch.Error.Code.ShouldBe("credit_insufficient");
        partly.Value.Customer.Balance.ShouldBe(200);
        partly.Value.Entries.Count.ShouldBe(2);
        partly.Value.Entries.Select(e => e.Kind).ShouldContain(CreditEntryKind.Refund);
    }

    [Fact]
    public async Task DeletingAPaymentReopensTheInvoiceAndTakesBackItsCredit()
    {
        var (fixture, customerId, invoice) = await SetupAsync();
        using var owner = fixture;
        var paid = (await PayAsync(fixture, invoice.Invoice.Id, 600)).Value.Invoice;

        var removed = await fixture.DeletePayment.HandleAsync(new DeletePayment.Command(invoice.Invoice.Id, paid.Payments.Single().Id), CancellationToken.None);

        removed.Value.Invoice.Status.ShouldBe(InvoiceStatus.Unpaid);
        removed.Value.CustomerCredit.ShouldBe(0);
        (await fixture.GetCredit.HandleAsync(new GetCustomerCredit.Query(customerId), CancellationToken.None)).Value.Entries.ShouldBeEmpty();
    }

    [Fact]
    public async Task APaymentWhoseCreditIsGoneCannotBeDeleted()
    {
        var (fixture, customerId, invoice) = await SetupAsync();
        using var owner = fixture;
        var paid = (await PayAsync(fixture, invoice.Invoice.Id, 600)).Value.Invoice;
        await fixture.RefundCredit.HandleAsync(new RefundCredit.Command(customerId, 100), CancellationToken.None);

        var removed = await fixture.DeletePayment.HandleAsync(new DeletePayment.Command(invoice.Invoice.Id, paid.Payments.Single().Id), CancellationToken.None);

        removed.Error.Code.ShouldBe("credit_insufficient");
    }

    [Fact]
    public async Task DeletingACreditPaymentGivesTheCreditBack()
    {
        var (fixture, customerId, first) = await SetupAsync();
        using var owner = fixture;
        await PayAsync(fixture, first.Invoice.Id, 700);
        var second = await InvoiceAsync(fixture, customerId);
        var applied = (await fixture.ApplyCredit.HandleAsync(new ApplyCredit.Command(second.Invoice.Id, 200), CancellationToken.None)).Value;

        var removed = await fixture.DeletePayment.HandleAsync(new DeletePayment.Command(second.Invoice.Id, applied.Payments.Single().Id), CancellationToken.None);

        removed.Value.CustomerCredit.ShouldBe(200);
        removed.Value.Invoice.Status.ShouldBe(InvoiceStatus.Unpaid);
    }

    [Fact]
    public async Task AnInvoiceWithPaymentsCannotBeCancelled()
    {
        var (fixture, _, invoice) = await SetupAsync();
        using var owner = fixture;
        var paid = (await PayAsync(fixture, invoice.Invoice.Id, 100)).Value.Invoice;

        var blocked = await fixture.CancelInvoice.HandleAsync(new CancelInvoice.Command(invoice.Invoice.Id), CancellationToken.None);
        await fixture.DeletePayment.HandleAsync(new DeletePayment.Command(invoice.Invoice.Id, paid.Payments.Single().Id), CancellationToken.None);
        var allowed = await fixture.CancelInvoice.HandleAsync(new CancelInvoice.Command(invoice.Invoice.Id), CancellationToken.None);
        var again = await fixture.CancelInvoice.HandleAsync(new CancelInvoice.Command(invoice.Invoice.Id), CancellationToken.None);

        blocked.Error.Code.ShouldBe("invoice_has_payments");
        allowed.Value.Invoice.Status.ShouldBe(InvoiceStatus.Cancelled);
        again.Error.Code.ShouldBe("invoice_already_cancelled");
    }

    [Fact]
    public async Task APartlyPaidInvoicePastItsDueDateIsOverdue()
    {
        var (fixture, _, invoice) = await SetupAsync();
        using var owner = fixture;
        await using (var db = fixture.DbFactory.CreateDbContext())
        {
            var entity = await db.Invoices.FindAsync([invoice.Invoice.Id], CancellationToken.None);
            entity!.DueDate = new DateOnly(2026, 11, 1);
            await db.SaveChangesAsync(CancellationToken.None);
        }

        await PayAsync(fixture, invoice.Invoice.Id, 100);
        var detail = await fixture.GetInvoice.HandleAsync(new GetInvoice.Query(invoice.Invoice.Id), CancellationToken.None);

        detail.Value.Invoice.IsOverdue.ShouldBeTrue();
        detail.Value.Invoice.Status.ShouldBe(InvoiceStatus.PartiallyPaid);
    }
}
