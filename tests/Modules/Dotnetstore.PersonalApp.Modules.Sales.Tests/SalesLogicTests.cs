using Dotnetstore.PersonalApp.Modules.Sales.Domain;
using Dotnetstore.PersonalApp.Modules.Sales.Features.Customers;
using Dotnetstore.PersonalApp.Modules.Sales.Features.Invoices;
using Dotnetstore.PersonalApp.Modules.Sales.Features.Offers;
using Dotnetstore.PersonalApp.Shared.Contacts;

namespace Dotnetstore.PersonalApp.Modules.Sales.Tests;

public sealed class OfferTotalsTests
{
    [Fact]
    public void ALineIsRoundedToTwoDecimalsAwayFromZero()
    {
        // 3 x 0.335 = 1.005 -> 1.01, not the banker's 1.00.
        OfferTotals.LineNet(new LineAmounts(3m, 0.335m, 25m, 0m)).ShouldBe(1.01m);
    }

    [Fact]
    public void ADiscountIsTakenOffTheNetAmountBeforeVat()
    {
        var line = new LineAmounts(2m, 100m, 25m, 10m);

        OfferTotals.LineNet(line).ShouldBe(180m);
        OfferTotals.LineVat(line).ShouldBe(45m);
    }

    [Fact]
    public void VatIsRoundedPerLineAndNotOnTheSum()
    {
        // Each line has 0.5 * 0.25 = 0.125 VAT, rounded to 0.13; rounding the sum (0.25) would give a different total.
        var lines = new[] { new LineAmounts(1m, 0.5m, 25m, 0m), new LineAmounts(1m, 0.5m, 25m, 0m) };

        var total = OfferTotals.Compute(lines);

        total.Net.ShouldBe(1m);
        total.Vat.ShouldBe(0.26m);
        total.Gross.ShouldBe(1.26m);
    }

    [Fact]
    public void NoLinesAddUpToNothing()
    {
        var total = OfferTotals.Compute([]);

        total.Net.ShouldBe(0m);
        total.Gross.ShouldBe(0m);
    }
}

public sealed class InvoicePaymentsTests
{
    private static Invoice InvoiceOf100(params decimal[] payments) => new()
    {
        Lines = [new InvoiceLine { Quantity = 1m, UnitPrice = 80m, VatRate = 25m }],
        Payments = [.. payments.Select(amount => new Payment { Amount = amount })],
    };

    [Fact]
    public void TotalIsTheGrossAmountOfTheLines() => InvoicePayments.Total(InvoiceOf100()).ShouldBe(100m);

    [Fact]
    public void OutstandingIsWhatIsLeftToPayAndNeverNegative()
    {
        InvoicePayments.Outstanding(InvoiceOf100(30m)).ShouldBe(70m);
        InvoicePayments.Outstanding(InvoiceOf100(60m, 60m)).ShouldBe(0m);
    }

    [Theory]
    [InlineData(0, "Unpaid")]
    [InlineData(40, "PartiallyPaid")]
    [InlineData(100, "Paid")]
    [InlineData(120, "Paid")]
    public void RefreshSetsTheStatusFromThePayments(int paid, string expected)
    {
        var invoice = paid == 0 ? InvoiceOf100() : InvoiceOf100(paid);

        InvoicePayments.Refresh(invoice);

        invoice.Status.ToString().ShouldBe(expected);
    }

    [Fact]
    public void ACancelledInvoiceStaysCancelledWhateverIsPaid()
    {
        var invoice = InvoiceOf100(100m);
        invoice.Status = InvoiceStatus.Cancelled;

        InvoicePayments.Refresh(invoice);

        invoice.Status.ShouldBe(InvoiceStatus.Cancelled);
    }
}

public sealed class CustomerAddressBlockTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task UsesTheStructuredLinesOfTheFirstAddress()
    {
        var source = new StubSource(ContactKind.Address, new ContactEntry(Guid.NewGuid(), ContactKind.Address, "Work", "ignored", ["Main St 1", "111 22 Stockholm"]));

        (await CustomerAddressBlock.ResolveAsync([source], Guid.NewGuid(), Ct)).ShouldBe("Main St 1\n111 22 Stockholm");
    }

    [Fact]
    public async Task SplitsAOneLineAddressAtTheCommas()
    {
        var source = new StubSource(null, new ContactEntry(Guid.NewGuid(), ContactKind.Address, "Work", "Main St 1, 111 22 Stockholm, Sweden"));

        (await CustomerAddressBlock.ResolveAsync([source], Guid.NewGuid(), Ct)).ShouldBe("Main St 1\n111 22 Stockholm\nSweden");
    }

    [Fact]
    public async Task ASourceThatOnlyOffersPhoneNumbersIsNotAsked()
    {
        var phones = new StubSource(ContactKind.Phone, new ContactEntry(Guid.NewGuid(), ContactKind.Phone, "Work", "070"));

        (await CustomerAddressBlock.ResolveAsync([phones], Guid.NewGuid(), Ct)).ShouldBeNull();
        phones.Asked.ShouldBeFalse();
    }

    [Fact]
    public async Task NoSourceMeansNoAddress() =>
        (await CustomerAddressBlock.ResolveAsync([], Guid.NewGuid(), Ct)).ShouldBeNull();

    private sealed class StubSource(ContactKind? kind, params ContactEntry[] entries) : IContactInfoSource
    {
        public bool Asked { get; private set; }

        public ContactKind? Kind => kind;

        public Task<IReadOnlyList<ContactEntry>> ListAsync(Guid objectId, CancellationToken cancellationToken)
        {
            Asked = true;
            return Task.FromResult<IReadOnlyList<ContactEntry>>(entries);
        }
    }
}
