using Dotnetstore.PersonalApp.Modules.TimeReports.Domain;
using Dotnetstore.PersonalApp.Modules.TimeReports.Features.Expenses;
using Dotnetstore.PersonalApp.Modules.TimeReports.Resources;
using Dotnetstore.PersonalApp.Shared.Auth;
using Dotnetstore.PersonalApp.Shared.Clients;
using Dotnetstore.PersonalApp.Shared.Files;
using Dotnetstore.PersonalApp.Shared.Localization;

namespace Dotnetstore.PersonalApp.Modules.TimeReports.Tests;

public sealed class ExpensesViewModelTests : IDisposable
{
    private static readonly byte[] PdfBytes = [0x25, 0x50, 0x44, 0x46, 0x2D];

    private readonly TimeReportsFixture _fixture = new();
    private readonly Localizer _localizer = new();
    private readonly AuthenticatedUser _user = new(Guid.NewGuid(), "anna", "Anna Andersson");
    private readonly FakePicker _picker = new();
    private readonly FakeSaver _saver = new();

    public ExpensesViewModelTests()
    {
        _localizer.AddResources(TimeReportsStrings.ResourceManager);
        _localizer.SetCulture("en");
    }

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public void Dispose() => _fixture.Dispose();

    private static async Task WaitUntilAsync(Func<bool> condition)
    {
        for (var i = 0; i < 500 && !condition(); i++)
        {
            await Task.Delay(10, Ct);
        }

        condition().ShouldBeTrue();
    }

    private string Error(string code) => _localizer.Get("Error_" + code);

    private static List<ExpenseCategoryOption> Categories() =>
        [new(ExpenseCategory.Hotel, "Hotel"), new(ExpenseCategory.Meals, "Meals"), new(ExpenseCategory.Other, "Other")];

    private ExpenseEditorViewModel Editor(ExpenseDto? existing = null, IReadOnlyList<ClientReference>? clients = null) =>
        new(existing, _user.Id, clients ?? [], Categories(), new DateOnly(2026, 10, 31),
            _fixture.CreateExpense, _fixture.UpdateExpense, _fixture.GetReceipt, _picker, _saver, _localizer);

    private async Task<ExpenseDto> AddExpenseAsync(string description = "Hotel", decimal amount = 1250m, ReceiptFile? receipt = null, DateOnly? date = null) =>
        (await _fixture.CreateExpense.HandleAsync(
            new CreateExpense.Command(_user.Id, null, date ?? new DateOnly(2026, 10, 30), ExpenseCategory.Hotel, description, amount, Math.Min(250m, amount / 5m), receipt), Ct)).Value;

    private ExpensesViewModel List(AuthenticatedUser? user) =>
        new(new FakeCurrentUser(user), _fixture.ListExpenses, _fixture.CreateExpense, _fixture.UpdateExpense, _fixture.DeleteExpense,
            _fixture.GetReceipt, _fixture.ExportExpenses, _picker, _saver, _fixture.Clients, _localizer, _fixture.Clock);

    // ---- editor ----

    [Fact]
    public void ANewEditorStartsOnTodayWithNoClientAndTheFirstCategory()
    {
        var client = _fixture.Clients.Add("Acme AB");

        var editor = Editor(null, [client]);

        editor.Title.ShouldBe(_localizer.Get("ExpenseEditor_New"));
        editor.Date.ShouldBe(new DateTime(2026, 10, 31));
        editor.Clients.Count.ShouldBe(2);
        editor.SelectedClient!.Id.ShouldBe(Guid.Empty);
        editor.SelectedCategory!.Category.ShouldBe(ExpenseCategory.Hotel);
        editor.Amount.ShouldBeNull();
        editor.VatAmount.ShouldBe(0m);
        editor.HasReceipt.ShouldBeFalse();
    }

    [Fact]
    public async Task AnExistingExpenseFillsTheEditor()
    {
        var client = _fixture.Clients.Add("Acme AB");
        var created = (await _fixture.CreateExpense.HandleAsync(
            new CreateExpense.Command(_user.Id, client.Id, new DateOnly(2026, 10, 2), ExpenseCategory.Meals, "Lunch", 150m, 18m, new ReceiptFile("l.pdf", PdfBytes)), Ct)).Value;

        var editor = Editor(created, [client]);

        editor.Title.ShouldBe(_localizer.Get("ExpenseEditor_Edit"));
        editor.SelectedClient.ShouldBe(client);
        editor.SelectedCategory!.Category.ShouldBe(ExpenseCategory.Meals);
        editor.Date.ShouldBe(new DateTime(2026, 10, 2));
        editor.Description.ShouldBe("Lunch");
        editor.Amount.ShouldBe(150m);
        editor.VatAmount.ShouldBe(18m);
        editor.ReceiptName.ShouldBe("l.pdf");
        editor.HasReceipt.ShouldBeTrue();
    }

    [Fact]
    public async Task SavingANewExpenseCreatesItAndRaisesSaved()
    {
        var editor = Editor();
        editor.Description = "Taxi";
        editor.Amount = 300m;
        editor.VatAmount = 36m;
        ExpenseDto? saved = null;
        editor.Saved += (_, dto) => saved = dto;

        await editor.SaveCommand.ExecuteAsync(null);

        saved.ShouldNotBeNull().Description.ShouldBe("Taxi");
        saved.ClientId.ShouldBeNull();
        editor.ErrorMessage.ShouldBeNull();
    }

    [Fact]
    public async Task SavingAnExistingExpenseUpdatesIt()
    {
        var created = await AddExpenseAsync();
        var editor = Editor(created);
        editor.Description = "Hotel, two nights";
        ExpenseDto? saved = null;
        editor.Saved += (_, dto) => saved = dto;

        await editor.SaveCommand.ExecuteAsync(null);

        saved.ShouldNotBeNull().Id.ShouldBe(created.Id);
        saved.Description.ShouldBe("Hotel, two nights");
    }

    [Fact]
    public async Task MissingFieldsAndInvalidAmountsAreReportedWithoutSaving()
    {
        var editor = Editor();
        var raised = false;
        editor.Saved += (_, _) => raised = true;

        await editor.SaveCommand.ExecuteAsync(null);
        editor.ErrorMessage.ShouldBe(Error("expense_amount_required"));

        editor.Date = null;
        await editor.SaveCommand.ExecuteAsync(null);
        editor.ErrorMessage.ShouldBe(Error("expense_date_required"));

        editor.Date = new DateTime(2026, 10, 30);
        editor.SelectedCategory = null;
        await editor.SaveCommand.ExecuteAsync(null);
        editor.ErrorMessage.ShouldBe(Error("expense_category_invalid"));

        editor.SelectedCategory = Editor().Categories[0];
        editor.Amount = 100m;
        await editor.SaveCommand.ExecuteAsync(null);
        editor.ErrorMessage.ShouldBe(Error("expense_description_required"));

        editor.Description = "x";
        editor.VatAmount = 500m;
        await editor.SaveCommand.ExecuteAsync(null);
        editor.ErrorMessage.ShouldBe(Error("expense_vat_invalid"));
        raised.ShouldBeFalse();
    }

    [Fact]
    public void CancelRaisesCancelled()
    {
        var editor = Editor();
        var cancelled = false;
        editor.Cancelled += (_, _) => cancelled = true;

        editor.CancelCommand.Execute(null);

        cancelled.ShouldBeTrue();
    }

    [Fact]
    public async Task AttachingAReceiptShowsItsNameAndSavesItWithTheExpense()
    {
        var editor = Editor();
        editor.Description = "Taxi";
        editor.Amount = 300m;
        _picker.Result = new FilePickResult([new PickedFile(@"C:\scans\taxi.PDF", PdfBytes)], []);
        ExpenseDto? saved = null;
        editor.Saved += (_, dto) => saved = dto;

        await editor.AttachReceiptCommand.ExecuteAsync(null);
        editor.ReceiptName.ShouldBe("taxi.PDF");
        editor.HasReceipt.ShouldBeTrue();
        _picker.LastMaxBytes.ShouldBe(ExpenseRules.MaxReceiptBytes);
        await editor.SaveCommand.ExecuteAsync(null);

        saved.ShouldNotBeNull().ReceiptFileName.ShouldBe("taxi.PDF");
        (await _fixture.GetReceipt.HandleAsync(new GetExpenseReceipt.Query(saved.Id, _user.Id), Ct)).Value.Content.ShouldBe(PdfBytes);
    }

    [Fact]
    public async Task ACancelledPickLeavesTheReceiptAlone()
    {
        var editor = Editor();
        _picker.Result = new FilePickResult([], []);

        await editor.AttachReceiptCommand.ExecuteAsync(null);

        editor.HasReceipt.ShouldBeFalse();
        editor.ReceiptStatus.ShouldBeNull();
    }

    [Fact]
    public async Task AnOversizedOrWrongTypeReceiptIsRejected()
    {
        var editor = Editor();

        _picker.Result = new FilePickResult([], ["huge.pdf"]);
        await editor.AttachReceiptCommand.ExecuteAsync(null);
        editor.ReceiptStatus.ShouldBe(Error("expense_receipt_too_large"));
        editor.HasReceipt.ShouldBeFalse();

        _picker.Result = new FilePickResult([new PickedFile("virus.exe", PdfBytes)], []);
        await editor.AttachReceiptCommand.ExecuteAsync(null);
        editor.ReceiptStatus.ShouldBe(Error("expense_receipt_type_invalid"));
        editor.HasReceipt.ShouldBeFalse();
    }

    [Fact]
    public async Task RemovingASavedReceiptDeletesItOnSave()
    {
        var created = await AddExpenseAsync(receipt: new ReceiptFile("a.pdf", PdfBytes));
        var editor = Editor(created);
        editor.RemoveReceiptCommand.CanExecute(null).ShouldBeTrue();

        editor.RemoveReceiptCommand.Execute(null);
        editor.HasReceipt.ShouldBeFalse();
        editor.RemoveReceiptCommand.CanExecute(null).ShouldBeFalse();
        await editor.SaveCommand.ExecuteAsync(null);

        (await _fixture.GetReceipt.HandleAsync(new GetExpenseReceipt.Query(created.Id, _user.Id), Ct)).IsSuccess.ShouldBeFalse();
    }

    [Fact]
    public async Task RemovingAReceiptPickedInThisFormDropsItWithoutTouchingTheSavedState()
    {
        var editor = Editor();
        _picker.Result = new FilePickResult([new PickedFile("a.pdf", PdfBytes)], []);
        await editor.AttachReceiptCommand.ExecuteAsync(null);

        editor.RemoveReceiptCommand.Execute(null);

        editor.HasReceipt.ShouldBeFalse();
        editor.SaveReceiptCopyCommand.CanExecute(null).ShouldBeFalse();
    }

    [Fact]
    public async Task ACopyOfASavedReceiptIsFetchedAndHandedToTheSaveDialog()
    {
        var created = await AddExpenseAsync(receipt: new ReceiptFile("a.pdf", PdfBytes));
        var editor = Editor(created);
        _saver.Path = @"C:\out\a.pdf";

        await editor.SaveReceiptCopyCommand.ExecuteAsync(null);

        _saver.LastName.ShouldBe("a.pdf");
        _saver.LastContent.ShouldBe(PdfBytes);
        editor.ReceiptStatus.ShouldBe(_localizer.Format("ExpenseEditor_ReceiptSaved", @"C:\out\a.pdf"));
    }

    [Fact]
    public async Task ACopyOfAReceiptPickedInThisFormNeedsNoDatabaseRead()
    {
        var editor = Editor();
        _picker.Result = new FilePickResult([new PickedFile("new.png", new byte[] { 1, 2, 3 })], []);
        await editor.AttachReceiptCommand.ExecuteAsync(null);
        _saver.Path = "saved.png";

        await editor.SaveReceiptCopyCommand.ExecuteAsync(null);

        _saver.LastContent.ShouldBe(new byte[] { 1, 2, 3 });
    }

    [Fact]
    public async Task CancellingTheSaveDialogShowsNoStatusAndAMissingReceiptShowsTheError()
    {
        var created = await AddExpenseAsync(receipt: new ReceiptFile("a.pdf", PdfBytes));
        var editor = Editor(created);
        _saver.Path = null;

        await editor.SaveReceiptCopyCommand.ExecuteAsync(null);
        editor.ReceiptStatus.ShouldBeNull();

        await _fixture.UpdateExpense.HandleAsync(
            new UpdateExpense.Command(created.Id, _user.Id, null, created.Date, created.Category, created.Description, created.Amount, created.VatAmount, null, true), Ct);
        await editor.SaveReceiptCopyCommand.ExecuteAsync(null);
        editor.ReceiptStatus.ShouldBe(Error("expense_receipt_not_found"));
    }

    // ---- list ----

    [Fact]
    public async Task LoadingWhileSignedOutShowsNotSignedIn()
    {
        var list = List(null);

        await list.LoadCommand.ExecuteAsync(null);
        list.NewCommand.Execute(null);

        list.StatusMessage.ShouldBe(_localizer.Get("Expenses_NotSignedIn"));
        list.Editor.ShouldBeNull();
        list.ShowEmptyState.ShouldBeTrue();
    }

    [Fact]
    public async Task TheListShowsExpensesWithCategoryLabelsAndATotal()
    {
        await AddExpenseAsync("Hotel", 1250m);
        await AddExpenseAsync("Dinner", 300m);
        var list = List(_user);

        await list.LoadCommand.ExecuteAsync(null);

        list.Expenses.Count.ShouldBe(2);
        list.Expenses.ShouldAllBe(r => r.CategoryLabel == _localizer.Get("ExpenseCategory_Hotel"));
        list.StatusMessage.ShouldBe(_localizer.Format("Expenses_Summary", 2, 1550m.ToString("N2", System.Globalization.CultureInfo.CurrentCulture)));
        list.ShowPager.ShouldBeFalse();
    }

    [Fact]
    public async Task OnlyTheSignedInUsersExpensesAreListed()
    {
        await AddExpenseAsync("Mine");
        await _fixture.CreateExpense.HandleAsync(
            new CreateExpense.Command(Guid.NewGuid(), null, new DateOnly(2026, 10, 1), ExpenseCategory.Other, "Not mine", 10m, 0m, null), Ct);
        var list = List(_user);

        await list.LoadCommand.ExecuteAsync(null);

        list.Expenses.Single().Expense.Description.ShouldBe("Mine");
    }

    [Fact]
    public async Task NewOpensAnEmptyEditorAndSelectingARowOpensItForEditing()
    {
        var created = await AddExpenseAsync();
        var list = List(_user);
        await list.LoadCommand.ExecuteAsync(null);

        list.NewCommand.Execute(null);
        list.Editor.ShouldNotBeNull().Title.ShouldBe(_localizer.Get("ExpenseEditor_New"));
        list.ShowEmptyState.ShouldBeFalse();

        list.SelectedExpense = list.Expenses.Single();
        list.Editor.ShouldNotBeNull().Title.ShouldBe(_localizer.Get("ExpenseEditor_Edit"));
        list.Editor.Description.ShouldBe(created.Description);
    }

    [Fact]
    public async Task CancellingTheEditorClosesItAndClearsTheSelection()
    {
        await AddExpenseAsync();
        var list = List(_user);
        await list.LoadCommand.ExecuteAsync(null);
        list.SelectedExpense = list.Expenses.Single();

        list.Editor!.CancelCommand.Execute(null);

        list.Editor.ShouldBeNull();
        list.SelectedExpense.ShouldBeNull();
    }

    [Fact]
    public async Task SavingFromTheEditorReloadsTheListAndConfirms()
    {
        var list = List(_user);
        await list.LoadCommand.ExecuteAsync(null);
        list.NewCommand.Execute(null);
        var editor = list.Editor!;
        editor.Description = "Taxi";
        editor.Amount = 200m;

        await editor.SaveCommand.ExecuteAsync(null);

        await WaitUntilAsync(() => list.StatusMessage == _localizer.Get("Expenses_Saved"));
        list.Expenses.Single().Expense.Description.ShouldBe("Taxi");
        list.Editor.ShouldBeNull();
    }

    [Fact]
    public async Task DeleteNeedsConfirmationAndCanBeWithdrawn()
    {
        await AddExpenseAsync();
        var list = List(_user);
        await list.LoadCommand.ExecuteAsync(null);
        list.RequestDeleteCommand.CanExecute(null).ShouldBeFalse();
        list.SelectedExpense = list.Expenses.Single();

        list.RequestDeleteCommand.Execute(null);
        list.IsConfirmingDelete.ShouldBeTrue();
        list.CancelDeleteCommand.Execute(null);
        list.IsConfirmingDelete.ShouldBeFalse();

        list.RequestDeleteCommand.Execute(null);
        await list.ConfirmDeleteCommand.ExecuteAsync(null);

        list.Expenses.ShouldBeEmpty();
        list.Editor.ShouldBeNull();
        list.IsConfirmingDelete.ShouldBeFalse();
    }

    [Fact]
    public async Task DeletingAnExpenseThatIsAlreadyGoneShowsTheError()
    {
        var created = await AddExpenseAsync();
        var list = List(_user);
        await list.LoadCommand.ExecuteAsync(null);
        list.SelectedExpense = list.Expenses.Single();
        await _fixture.DeleteExpense.HandleAsync(new DeleteExpense.Command(created.Id, _user.Id), Ct);

        await list.ConfirmDeleteCommand.ExecuteAsync(null);

        list.StatusMessage.ShouldBe(Error("expense_not_found"));
    }

    [Fact]
    public async Task ManyExpensesArePagedAndTheLastPageStepsBackAfterDeletingItsOnlyRow()
    {
        for (var i = 0; i < 16; i++)
        {
            await AddExpenseAsync("E" + i, 10m + i, date: new DateOnly(2026, 1, 1).AddDays(i));
        }

        var list = List(_user);
        await list.LoadCommand.ExecuteAsync(null);
        list.PageCount.ShouldBe(2);
        list.ShowPager.ShouldBeTrue();
        list.Expenses.Count.ShouldBe(15);
        list.PreviousPageCommand.CanExecute(null).ShouldBeFalse();
        list.PageLabel.ShouldBe(_localizer.Format("TimeReports_PageOf", 1, 2));

        await list.NextPageCommand.ExecuteAsync(null);
        list.CurrentPage.ShouldBe(2);
        list.Expenses.Count.ShouldBe(1);
        list.NextPageCommand.CanExecute(null).ShouldBeFalse();

        list.SelectedExpense = list.Expenses.Single();
        await list.ConfirmDeleteCommand.ExecuteAsync(null);

        list.CurrentPage.ShouldBe(1);
        list.PageCount.ShouldBe(1);
        list.Expenses.Count.ShouldBe(15);

        // Paging to the previous page is a no-op at page 1 and a step back above it.
        await list.PreviousPageCommand.ExecuteAsync(null).ConfigureAwait(true);
    }

    [Fact]
    public async Task ExportingSavesAWorkbookOfTheYearAndReportsTheCount()
    {
        await AddExpenseAsync();
        var list = List(_user);
        _saver.Path = @"C:\out\expenses.xlsx";

        await list.ExportCommand.ExecuteAsync(null);

        _saver.LastName.ShouldNotBeNull().ShouldEndWith(".xlsx");
        _saver.LastContent.ShouldNotBeNull().Length.ShouldBeGreaterThan(0);
        list.StatusMessage.ShouldBe(_localizer.Format("Expenses_Exported", 1, @"C:\out\expenses.xlsx"));
    }

    [Fact]
    public async Task ExportingWithABadYearOrWhileSignedOutDoesNotOpenTheSaveDialog()
    {
        var list = List(_user);

        list.ExportYear = null;
        await list.ExportCommand.ExecuteAsync(null);
        list.StatusMessage.ShouldBe(Error("time_report_year_invalid"));

        list.ExportYear = 1m;
        await list.ExportCommand.ExecuteAsync(null);
        list.StatusMessage.ShouldBe(Error("time_report_year_invalid"));

        var signedOut = List(null);
        await signedOut.ExportCommand.ExecuteAsync(null);
        signedOut.StatusMessage.ShouldBe(_localizer.Get("Expenses_NotSignedIn"));

        _saver.Calls.ShouldBe(0);
    }

    [Fact]
    public async Task CancellingTheExportDialogLeavesTheStatusAlone()
    {
        await AddExpenseAsync();
        var list = List(_user);
        _saver.Path = null;

        await list.ExportCommand.ExecuteAsync(null);

        list.StatusMessage.ShouldBeNull();
        _saver.Calls.ShouldBe(1);
    }

    [Fact]
    public void TheExportYearDefaultsToTheCurrentYear() => List(_user).ExportYear.ShouldBe(2026m);

    private sealed class FakeCurrentUser(AuthenticatedUser? user) : ICurrentUser
    {
        public AuthenticatedUser? User { get; } = user;
    }

    private sealed class FakePicker : IFilePickService
    {
        public FilePickResult Result { get; set; } = new([], []);

        public long LastMaxBytes { get; private set; }

        public Task<FilePickResult> PickFilesAsync(string title, long maxBytes, CancellationToken cancellationToken)
        {
            LastMaxBytes = maxBytes;
            return Task.FromResult(Result);
        }
    }

    private sealed class FakeSaver : IFileSaveService
    {
        public string? Path { get; set; }

        public string? LastName { get; private set; }

        public byte[]? LastContent { get; private set; }

        public int Calls { get; private set; }

        public Task<string?> SaveAsync(string title, string suggestedFileName, byte[] content, CancellationToken cancellationToken)
        {
            Calls++;
            LastName = suggestedFileName;
            LastContent = content;
            return Task.FromResult(Path);
        }
    }
}
