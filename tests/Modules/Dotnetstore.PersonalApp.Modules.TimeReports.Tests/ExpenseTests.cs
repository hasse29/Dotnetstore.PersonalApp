using ClosedXML.Excel;
using Dotnetstore.PersonalApp.Modules.TimeReports.Domain;
using Dotnetstore.PersonalApp.Modules.TimeReports.Features.Expenses;

namespace Dotnetstore.PersonalApp.Modules.TimeReports.Tests;

public sealed class ExpenseTests : IDisposable
{
    private static readonly byte[] PdfBytes = [0x25, 0x50, 0x44, 0x46, 0x2D];

    private readonly TimeReportsFixture _fixture = new();
    private readonly Guid _user = Guid.NewGuid();

    public void Dispose() => _fixture.Dispose();

    private static CreateExpense.Command Valid(Guid userId, Guid? clientId = null) =>
        new(userId, clientId, new DateOnly(2026, 10, 30), ExpenseCategory.Hotel, "Hotel in Gothenburg", 1250m, 250m, null);

    [Fact]
    public async Task CreatesAnExpenseWithAReceiptAndReadsTheReceiptBack()
    {
        var ct = TestContext.Current.CancellationToken;
        var client = _fixture.Clients.Add("Acme AB");

        var created = await _fixture.CreateExpense.HandleAsync(
            Valid(_user, client.Id) with { Receipt = new ReceiptFile(@"C:\scans\hotel.PDF", PdfBytes) }, ct);

        created.IsSuccess.ShouldBeTrue();
        created.Value.ClientName.ShouldBe("Acme AB");
        created.Value.ReceiptFileName.ShouldBe("hotel.PDF");

        var receipt = await _fixture.GetReceipt.HandleAsync(new GetExpenseReceipt.Query(created.Value.Id, _user), ct);
        receipt.IsSuccess.ShouldBeTrue();
        receipt.Value.Content.ShouldBe(PdfBytes);
    }

    [Fact]
    public async Task RejectsInvalidInput()
    {
        var ct = TestContext.Current.CancellationToken;

        (await _fixture.CreateExpense.HandleAsync(Valid(_user) with { Description = "  " }, ct)).Error.Code.ShouldBe("expense_description_required");
        (await _fixture.CreateExpense.HandleAsync(Valid(_user) with { Amount = 0m }, ct)).Error.Code.ShouldBe("expense_amount_invalid");
        (await _fixture.CreateExpense.HandleAsync(Valid(_user) with { VatAmount = 2000m }, ct)).Error.Code.ShouldBe("expense_vat_invalid");
        (await _fixture.CreateExpense.HandleAsync(Valid(_user) with { ClientId = Guid.NewGuid() }, ct)).Error.Code.ShouldBe("expense_client_not_found");
        (await _fixture.CreateExpense.HandleAsync(Valid(_user) with { Receipt = new ReceiptFile("receipt.exe", PdfBytes) }, ct)).Error.Code.ShouldBe("expense_receipt_type_invalid");
        (await _fixture.CreateExpense.HandleAsync(Valid(_user) with { Receipt = new ReceiptFile("receipt.pdf", []) }, ct)).Error.Code.ShouldBe("expense_receipt_empty");
    }

    [Fact]
    public async Task UpdateKeepsReplacesAndRemovesTheReceipt()
    {
        var ct = TestContext.Current.CancellationToken;
        var created = (await _fixture.CreateExpense.HandleAsync(Valid(_user) with { Receipt = new ReceiptFile("a.pdf", PdfBytes) }, ct)).Value;

        UpdateExpense.Command Change(ReceiptFile? receipt, bool remove) =>
            new(created.Id, _user, null, created.Date, ExpenseCategory.Meals, "Dinner", 300m, 36m, receipt, remove);

        var kept = (await _fixture.UpdateExpense.HandleAsync(Change(null, false), ct)).Value;
        kept.ReceiptFileName.ShouldBe("a.pdf");
        kept.Category.ShouldBe(ExpenseCategory.Meals);

        var replaced = (await _fixture.UpdateExpense.HandleAsync(Change(new ReceiptFile("b.png", [1, 2, 3]), false), ct)).Value;
        replaced.ReceiptFileName.ShouldBe("b.png");
        (await _fixture.GetReceipt.HandleAsync(new GetExpenseReceipt.Query(created.Id, _user), ct)).Value.Content.ShouldBe(new byte[] { 1, 2, 3 });

        var removed = (await _fixture.UpdateExpense.HandleAsync(Change(null, true), ct)).Value;
        removed.HasReceipt.ShouldBeFalse();
        (await _fixture.GetReceipt.HandleAsync(new GetExpenseReceipt.Query(created.Id, _user), ct)).Error.Code.ShouldBe("expense_receipt_not_found");
    }

    [Fact]
    public async Task OtherUsersCannotChangeReadOrDeleteAnExpense()
    {
        var ct = TestContext.Current.CancellationToken;
        var created = (await _fixture.CreateExpense.HandleAsync(Valid(_user) with { Receipt = new ReceiptFile("a.pdf", PdfBytes) }, ct)).Value;
        var other = Guid.NewGuid();

        var update = new UpdateExpense.Command(created.Id, other, null, created.Date, ExpenseCategory.Other, "x", 1m, 0m, null, false);
        (await _fixture.UpdateExpense.HandleAsync(update, ct)).Error.Code.ShouldBe("expense_not_found");
        (await _fixture.GetReceipt.HandleAsync(new GetExpenseReceipt.Query(created.Id, other), ct)).Error.Code.ShouldBe("expense_receipt_not_found");
        (await _fixture.DeleteExpense.HandleAsync(new DeleteExpense.Command(created.Id, other), ct)).Error.Code.ShouldBe("expense_not_found");
        (await _fixture.ListExpenses.HandleAsync(new ListExpenses.Query(_user), ct)).TotalCount.ShouldBe(1);
    }

    [Fact]
    public async Task DeleteRemovesTheReceiptToo()
    {
        var ct = TestContext.Current.CancellationToken;
        var created = (await _fixture.CreateExpense.HandleAsync(Valid(_user) with { Receipt = new ReceiptFile("a.pdf", PdfBytes) }, ct)).Value;

        (await _fixture.DeleteExpense.HandleAsync(new DeleteExpense.Command(created.Id, _user), ct)).IsSuccess.ShouldBeTrue();

        await using var db = _fixture.DbFactory.CreateDbContext();
        db.ExpenseReceipts.Count().ShouldBe(0);
    }

    [Fact]
    public async Task ListsNewestFirstWithATotalOverEveryPage()
    {
        var ct = TestContext.Current.CancellationToken;
        await _fixture.CreateExpense.HandleAsync(Valid(_user) with { Date = new DateOnly(2026, 1, 1), Amount = 100m, VatAmount = 0m }, ct);
        await _fixture.CreateExpense.HandleAsync(Valid(_user) with { Date = new DateOnly(2026, 3, 1), Amount = 50m, VatAmount = 0m, Receipt = new ReceiptFile("a.pdf", PdfBytes) }, ct);

        var page = await _fixture.ListExpenses.HandleAsync(new ListExpenses.Query(_user, 1, 1), ct);

        page.TotalCount.ShouldBe(2);
        page.TotalAmount.ShouldBe(150m);
        var first = page.Items.ShouldHaveSingleItem();
        first.Date.ShouldBe(new DateOnly(2026, 3, 1));
        first.ReceiptFileName.ShouldBe("a.pdf");
    }

    [Fact]
    public async Task ExportsOneYearWithTotalsAndACategorySummary()
    {
        var ct = TestContext.Current.CancellationToken;
        var client = _fixture.Clients.Add("Acme AB");
        await _fixture.CreateExpense.HandleAsync(Valid(_user, client.Id) with { Date = new DateOnly(2026, 2, 1), Amount = 1000m, VatAmount = 200m, Receipt = new ReceiptFile("hotel.pdf", PdfBytes) }, ct);
        await _fixture.CreateExpense.HandleAsync(Valid(_user) with { Date = new DateOnly(2026, 6, 1), Category = ExpenseCategory.Hotel, Amount = 500m, VatAmount = 100m }, ct);
        await _fixture.CreateExpense.HandleAsync(Valid(_user) with { Date = new DateOnly(2026, 7, 1), Category = ExpenseCategory.Fuel, Amount = 300m, VatAmount = 60m }, ct);
        await _fixture.CreateExpense.HandleAsync(Valid(_user) with { Date = new DateOnly(2025, 12, 31), Amount = 999m, VatAmount = 0m }, ct);
        await _fixture.CreateExpense.HandleAsync(Valid(Guid.NewGuid()) with { Date = new DateOnly(2026, 2, 2), Amount = 999m, VatAmount = 0m }, ct);

        var result = await _fixture.ExportExpenses.HandleAsync(new ExportExpenses.Query(_user, 2026), ct);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ExpenseCount.ShouldBe(3);
        using var workbook = new XLWorkbook(new MemoryStream(result.Value.Content));
        workbook.Worksheets.Count.ShouldBe(2);

        var list = workbook.Worksheet(1);
        list.Cell(4, 4).GetString().ShouldBe("Acme AB");
        list.Cell(4, 5).GetValue<decimal>().ShouldBe(1000m);
        list.Cell(4, 7).GetString().ShouldBe("hotel.pdf");
        ((decimal)list.Cell(7, 5).Value.GetNumber()).ShouldBe(1800m);
        ((decimal)list.Cell(7, 6).Value.GetNumber()).ShouldBe(360m);

        var summary = workbook.Worksheet(2);
        summary.Cell(4, 2).GetValue<decimal>().ShouldBe(1500m);
        summary.Cell(5, 2).GetValue<decimal>().ShouldBe(300m);
        ((decimal)summary.Cell(6, 2).Value.GetNumber()).ShouldBe(1800m);
    }

    [Fact]
    public async Task ExportReportsAnEmptyOrInvalidYear()
    {
        var ct = TestContext.Current.CancellationToken;

        (await _fixture.ExportExpenses.HandleAsync(new ExportExpenses.Query(_user, 2026), ct)).Error.Code.ShouldBe("expense_export_empty");
        (await _fixture.ExportExpenses.HandleAsync(new ExportExpenses.Query(_user, 1900), ct)).Error.Code.ShouldBe("time_report_year_invalid");
    }
}
