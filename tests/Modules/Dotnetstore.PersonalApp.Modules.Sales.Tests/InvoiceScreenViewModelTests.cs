using System.Globalization;
using Dotnetstore.PersonalApp.Modules.Sales.Features.Invoices;

namespace Dotnetstore.PersonalApp.Modules.Sales.Tests;

/// <summary>Invoices of 500 (4 hours at 100 + 25 % VAT).</summary>
public sealed class InvoiceScreenViewModelTests : IDisposable
{
    private readonly ScreenHarness _harness = new();

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public void Dispose() => _harness.Dispose();

    private static string Money(decimal amount) => amount.ToString("N2", CultureInfo.CurrentCulture);

    private async Task<(InvoiceScreenViewModel Screen, InvoiceDetailDto Invoice)> OpenAsync(Guid? customerId = null)
    {
        var invoice = await _harness.InvoiceAsync(customerId);
        var screen = _harness.InvoiceScreen(invoice.Invoice.Id);
        await screen.LoadCommand.ExecuteAsync(null);
        return (screen, invoice);
    }

    [Fact]
    public async Task LoadShowsTheInvoiceItsTotalsAndWhatIsLeftToPay()
    {
        var (screen, invoice) = await OpenAsync();

        screen.Heading.ShouldBe(invoice.Invoice.Number);
        screen.StatusText.ShouldBe(_harness.Localizer.Get("InvoiceStatus_Unpaid"));
        screen.NetText.ShouldBe(Money(400));
        screen.VatText.ShouldBe(Money(100));
        screen.TotalText.ShouldBe(Money(500));
        screen.PaidText.ShouldBe(Money(0));
        screen.OutstandingText.ShouldBe(Money(500));
        screen.Lines.Single().VatText.ShouldBe(string.Create(CultureInfo.CurrentCulture, $"{25:0.##} %"));
        screen.OrderText.ShouldBe(_harness.Localizer.Format("InvoiceScreen_FromOrder", (object)invoice.Invoice.OrderNumber));
        screen.HasPayments.ShouldBeFalse();
        screen.CanPay.ShouldBeTrue();
        screen.CanCancel.ShouldBeTrue();
        screen.CanApplyCredit.ShouldBeFalse();
        screen.CreditText.ShouldBeEmpty();
        screen.PaymentAmount.ShouldBe(500);
    }

    [Fact]
    public async Task AnUnknownInvoiceShowsTheErrorAndNothingElse()
    {
        var screen = _harness.InvoiceScreen(Guid.NewGuid());

        await screen.LoadCommand.ExecuteAsync(null);

        screen.StatusMessage.ShouldBe(_harness.Localizer.Get("Error_invoice_not_found"));
        screen.Heading.ShouldBeEmpty();
        screen.CanPay.ShouldBeFalse();
        screen.CanCancel.ShouldBeFalse();
    }

    [Fact]
    public async Task APartPaymentIsRecordedAndTheRestIsOfferedAsTheNextAmount()
    {
        var (screen, _) = await OpenAsync();
        screen.PaymentAmount = 200;
        screen.PaymentNote = "Bank ref 1";

        await screen.RecordPaymentCommand.ExecuteAsync(null);

        screen.StatusMessage.ShouldBe(_harness.Localizer.Get("InvoiceScreen_PaymentRecorded"));
        screen.PaidText.ShouldBe(Money(200));
        screen.OutstandingText.ShouldBe(Money(300));
        screen.StatusText.ShouldBe(_harness.Localizer.Get("InvoiceStatus_PartiallyPaid"));
        screen.Payments.Single().KindText.ShouldContain("Bank ref 1");
        screen.Payments.Single().AmountText.ShouldBe(Money(200));
        screen.Payments.Single().ExcessText.ShouldBeNull();
        screen.PaymentAmount.ShouldBe(300);
        screen.PaymentNote.ShouldBeNull();
        screen.PaymentDate.ShouldBeNull();
        screen.CanCancel.ShouldBeFalse();
    }

    [Fact]
    public async Task PayingTheWholeAmountPaysTheInvoiceAndClearsTheAmountField()
    {
        var (screen, _) = await OpenAsync();

        await screen.RecordPaymentCommand.ExecuteAsync(null);

        screen.StatusText.ShouldBe(_harness.Localizer.Get("InvoiceStatus_Paid"));
        screen.OutstandingText.ShouldBe(Money(0));
        screen.PaymentAmount.ShouldBeNull();
    }

    [Fact]
    public async Task APaymentCanBeDatedAndTheDateIsKept()
    {
        var (screen, _) = await OpenAsync();
        screen.PaymentAmount = 100;
        screen.PaymentDate = new DateTime(2026, 11, 10);

        await screen.RecordPaymentCommand.ExecuteAsync(null);

        screen.Payments.Single().DateText.ShouldBe(new DateOnly(2026, 11, 10).ToString("d", CultureInfo.CurrentCulture));
    }

    [Fact]
    public async Task APaymentCannotBeDatedBeforeTheInvoice()
    {
        var (screen, _) = await OpenAsync();
        screen.PaymentAmount = 100;
        screen.PaymentDate = new DateTime(2026, 11, 5);

        await screen.RecordPaymentCommand.ExecuteAsync(null);

        screen.StatusMessage.ShouldBe(_harness.Localizer.Get("Error_payment_date_invalid"));
        screen.HasPayments.ShouldBeFalse();
    }

    [Fact]
    public async Task PayingTooMuchIsAllowedAndTheExcessBecomesCredit()
    {
        var (screen, _) = await OpenAsync();
        screen.PaymentAmount = 600;

        await screen.RecordPaymentCommand.ExecuteAsync(null);

        screen.StatusMessage.ShouldBe(_harness.Localizer.Format("InvoiceScreen_Overpaid", Money(100)));
        screen.StatusText.ShouldBe(_harness.Localizer.Get("InvoiceStatus_Paid"));
        screen.CreditText.ShouldBe(_harness.Localizer.Format("InvoiceScreen_Credit", Money(100)));
        screen.Payments.Single().ExcessText.ShouldBe(_harness.Localizer.Format("InvoiceScreen_PaymentExcess", Money(100)));
        screen.Payments.Single().AmountText.ShouldBe(Money(600));
    }

    [Fact]
    public async Task AnEmptyAmountIsRejectedBeforeAnythingIsSent()
    {
        var (screen, _) = await OpenAsync();
        screen.PaymentAmount = null;

        await screen.RecordPaymentCommand.ExecuteAsync(null);

        screen.StatusMessage.ShouldBe(_harness.Localizer.Get("Error_payment_amount_invalid"));
        screen.HasPayments.ShouldBeFalse();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-50)]
    [InlineData(20_000_000)]
    public async Task AnAmountOutsideTheAllowedRangeIsRejectedByTheHandler(int amount)
    {
        var (screen, _) = await OpenAsync();
        screen.PaymentAmount = amount;

        await screen.RecordPaymentCommand.ExecuteAsync(null);

        screen.StatusMessage.ShouldBe(_harness.Localizer.Get("Error_payment_amount_invalid"));
        screen.HasPayments.ShouldBeFalse();
        screen.PaidText.ShouldBe(Money(0));
    }

    [Fact]
    public async Task ACancelledInvoiceCannotBePaid()
    {
        var (screen, _) = await OpenAsync();
        await screen.ConfirmCancelCommand.ExecuteAsync(null);
        screen.CanPay.ShouldBeFalse();
        screen.PaymentAmount = 100;

        await screen.RecordPaymentCommand.ExecuteAsync(null);

        screen.StatusMessage.ShouldBe(_harness.Localizer.Get("Error_invoice_cancelled"));
        screen.HasPayments.ShouldBeFalse();
    }

    [Fact]
    public async Task CreditFromAnOverpaymentCanBeUsedOnAnotherInvoiceOfTheSameCustomer()
    {
        var customer = await _harness.Fixture.NewCustomerAsync("Credit School");
        var (first, _) = await OpenAsync(customer.Id);
        first.PaymentAmount = 600;
        await first.RecordPaymentCommand.ExecuteAsync(null);
        var (second, _) = await OpenAsync(customer.Id);

        second.CanApplyCredit.ShouldBeTrue();
        second.CreditText.ShouldBe(_harness.Localizer.Format("InvoiceScreen_Credit", Money(100)));

        await second.ApplyCreditCommand.ExecuteAsync(null);

        second.StatusMessage.ShouldBe(_harness.Localizer.Format("InvoiceScreen_CreditApplied", Money(100)));
        second.PaidText.ShouldBe(Money(100));
        second.OutstandingText.ShouldBe(Money(400));
        second.CanApplyCredit.ShouldBeFalse();
        second.CreditText.ShouldBeEmpty();
    }

    [Fact]
    public async Task OnlyWhatIsLeftToPayIsTakenFromALargerCredit()
    {
        var customer = await _harness.Fixture.NewCustomerAsync("Rich School");
        var (first, _) = await OpenAsync(customer.Id);
        first.PaymentAmount = 1500;
        await first.RecordPaymentCommand.ExecuteAsync(null);
        var (second, _) = await OpenAsync(customer.Id);

        await second.ApplyCreditCommand.ExecuteAsync(null);

        second.StatusText.ShouldBe(_harness.Localizer.Get("InvoiceStatus_Paid"));
        second.StatusMessage.ShouldBe(_harness.Localizer.Format("InvoiceScreen_CreditApplied", Money(500)));
        second.CreditText.ShouldBe(_harness.Localizer.Format("InvoiceScreen_Credit", Money(500)));
    }

    [Fact]
    public async Task WithoutCreditThereIsNothingToApply()
    {
        var (screen, _) = await OpenAsync();

        await screen.ApplyCreditCommand.ExecuteAsync(null);

        screen.CanApplyCredit.ShouldBeFalse();
        screen.StatusMessage.ShouldBe(_harness.Localizer.Get("Error_payment_amount_invalid"));
        screen.HasPayments.ShouldBeFalse();
    }

    [Fact]
    public async Task ApplyingCreditBeforeTheInvoiceIsLoadedDoesNothing()
    {
        var screen = _harness.InvoiceScreen((await _harness.InvoiceAsync()).Invoice.Id);

        await screen.ApplyCreditCommand.ExecuteAsync(null);

        screen.StatusMessage.ShouldBeNull();
    }

    [Fact]
    public async Task ARemovedPaymentGivesTheAmountBack()
    {
        var (screen, _) = await OpenAsync();
        screen.PaymentAmount = 200;
        await screen.RecordPaymentCommand.ExecuteAsync(null);

        await screen.DeletePaymentCommand.ExecuteAsync(screen.Payments.Single());

        screen.StatusMessage.ShouldBe(_harness.Localizer.Get("InvoiceScreen_PaymentRemoved"));
        screen.HasPayments.ShouldBeFalse();
        screen.OutstandingText.ShouldBe(Money(500));
        screen.StatusText.ShouldBe(_harness.Localizer.Get("InvoiceStatus_Unpaid"));
        screen.CanCancel.ShouldBeTrue();
    }

    [Fact]
    public async Task RemovingNothingDoesNothing()
    {
        var (screen, _) = await OpenAsync();

        await screen.DeletePaymentCommand.ExecuteAsync(null);

        screen.StatusMessage.ShouldBeNull();
    }

    [Fact]
    public async Task APaymentWhoseCreditHasBeenUsedCannotBeRemoved()
    {
        var customer = await _harness.Fixture.NewCustomerAsync("Used School");
        var (first, _) = await OpenAsync(customer.Id);
        first.PaymentAmount = 600;
        await first.RecordPaymentCommand.ExecuteAsync(null);
        var (second, _) = await OpenAsync(customer.Id);
        await second.ApplyCreditCommand.ExecuteAsync(null);

        await first.DeletePaymentCommand.ExecuteAsync(first.Payments.Single());

        first.StatusMessage.ShouldBe(_harness.Localizer.Get("Error_credit_insufficient"));
        first.HasPayments.ShouldBeTrue();
    }

    [Fact]
    public async Task CancellingNeedsAConfirmationThatCanBeWithdrawn()
    {
        var (screen, _) = await OpenAsync();

        screen.RequestCancelCommand.Execute(null);
        screen.IsConfirmingCancel.ShouldBeTrue();
        screen.CancelCancelCommand.Execute(null);

        screen.IsConfirmingCancel.ShouldBeFalse();
        screen.CanCancel.ShouldBeTrue();
    }

    [Fact]
    public async Task ConfirmingCancelsTheInvoiceAndLetsTheOrderBeInvoicedAgain()
    {
        var (screen, invoice) = await OpenAsync();
        screen.RequestCancelCommand.Execute(null);

        await screen.ConfirmCancelCommand.ExecuteAsync(null);

        screen.StatusMessage.ShouldBe(_harness.Localizer.Get("InvoiceScreen_Cancelled"));
        screen.StatusText.ShouldBe(_harness.Localizer.Get("InvoiceStatus_Cancelled"));
        screen.IsConfirmingCancel.ShouldBeFalse();
        screen.OutstandingText.ShouldBe(Money(0));
        screen.CanPay.ShouldBeFalse();
        screen.CanCancel.ShouldBeFalse();
        (await _harness.Fixture.GetInvoice.HandleAsync(new GetInvoice.Query(invoice.Invoice.Id), Ct)).Value.Invoice.Status.ToString().ShouldBe("Cancelled");
    }

    [Fact]
    public async Task AnInvoiceWithPaymentsCannotBeCancelled()
    {
        var (screen, _) = await OpenAsync();
        screen.PaymentAmount = 100;
        await screen.RecordPaymentCommand.ExecuteAsync(null);
        screen.CanCancel.ShouldBeFalse();

        await screen.ConfirmCancelCommand.ExecuteAsync(null);

        screen.StatusMessage.ShouldBe(_harness.Localizer.Get("Error_invoice_has_payments"));
        screen.StatusText.ShouldBe(_harness.Localizer.Get("InvoiceStatus_PartiallyPaid"));
    }

    [Fact]
    public async Task BackTellsTheHostToReturnToTheList()
    {
        var screen = _harness.InvoiceScreen(Guid.NewGuid());
        var closed = false;
        screen.Closed += (_, _) => closed = true;

        screen.BackCommand.Execute(null);

        closed.ShouldBeTrue();
    }

    [Fact]
    public async Task ExportPdfSavesThePdfAndTellsWhereItWent()
    {
        var (screen, _) = await OpenAsync();

        await screen.ExportPdfCommand.ExecuteAsync(null);

        _harness.FileSave.Content.ShouldNotBeNull().Length.ShouldBeGreaterThan(100);
        screen.StatusMessage.ShouldBe(_harness.Localizer.Format("SalesPdf_Saved", @"C:\temp\document.pdf"));
    }

    [Fact]
    public async Task CancellingTheSaveDialogLeavesTheStatusAlone()
    {
        var (screen, _) = await OpenAsync();
        _harness.FileSave.PickedPath = null;

        await screen.ExportPdfCommand.ExecuteAsync(null);

        screen.StatusMessage.ShouldBeNull();
    }
}
