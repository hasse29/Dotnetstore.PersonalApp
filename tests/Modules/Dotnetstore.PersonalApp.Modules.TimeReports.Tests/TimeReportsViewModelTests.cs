using System.Globalization;
using Dotnetstore.PersonalApp.Modules.TimeReports.Features.TimeReports;
using Dotnetstore.PersonalApp.Modules.TimeReports.Resources;
using Dotnetstore.PersonalApp.Shared.Auth;
using Dotnetstore.PersonalApp.Shared.Clients;
using Dotnetstore.PersonalApp.Shared.Email;
using Dotnetstore.PersonalApp.Shared.Files;
using Dotnetstore.PersonalApp.Shared.Localization;
using Dotnetstore.PersonalApp.Shared.Results;

namespace Dotnetstore.PersonalApp.Modules.TimeReports.Tests;

public sealed class TimeReportsViewModelTests : IDisposable
{
    private readonly TimeReportsFixture _fixture = new();
    private readonly Localizer _localizer = new();
    private readonly AuthenticatedUser _user = new(Guid.NewGuid(), "anna", "Anna Andersson");
    private readonly FakeSaver _saver = new();
    private readonly ClientReference _acme;
    private readonly ClientReference _beta;

    public TimeReportsViewModelTests()
    {
        _localizer.AddResources(TimeReportsStrings.ResourceManager);
        _localizer.SetCulture("en");
        _acme = _fixture.Clients.Add("Acme AB");
        _beta = _fixture.Clients.Add("Beta AB");
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

    private TimeReportsViewModel List(Guid? fixedClientId = null, AuthenticatedUser? user = null, bool signedIn = true, ISystemMailSender? mail = null) =>
        new(fixedClientId, new FakeCurrentUser(signedIn ? user ?? _user : null), _fixture.List, _fixture.Create, _fixture.Update, _fixture.Delete,
            _fixture.Export, _fixture.ExportKilometers, _saver, _fixture.ListTypes, _fixture.Clients, _localizer, _fixture.Clock, mail);

    private async Task<TimeReportDto> AddReportAsync(ClientReference client, DateOnly? date = null, decimal hours = 8m, decimal km = 42.5m) =>
        (await _fixture.Create.HandleAsync(
            TimeReportsFixture.ValidCommand(_user.Id, client.Id) with { Date = date ?? new DateOnly(2026, 10, 30), TotalHours = hours, DrivingKilometers = km }, Ct)).Value;

    [Fact]
    public async Task LoadingWhileSignedOutShowsNotSignedIn()
    {
        var list = List(signedIn: false);

        await list.LoadCommand.ExecuteAsync(null);
        list.NewCommand.Execute(null);
        await list.ExportCommand.ExecuteAsync(null);
        await list.ExportKilometersCommand.ExecuteAsync(null);

        list.StatusMessage.ShouldBe(_localizer.Get("TimeReports_NotSignedIn"));
        list.Editor.ShouldBeNull();
        list.Reports.ShouldBeEmpty();
    }

    [Fact]
    public async Task TheListShowsTheReportsWithATotalAndTheClientFilter()
    {
        await AddReportAsync(_acme);
        await AddReportAsync(_beta, new DateOnly(2026, 10, 29), 4m, 0m);
        var list = List();

        await list.LoadCommand.ExecuteAsync(null);

        list.Reports.Count.ShouldBe(2);
        list.FilterClients.Select(c => c.Name).ShouldBe([_localizer.Get("TimeReports_AllClients"), "Acme AB", "Beta AB"]);
        list.SelectedFilter!.Id.ShouldBe(Guid.Empty);
        list.IsPageLayout.ShouldBeTrue();
        list.CanEmail.ShouldBeFalse();
        list.StatusMessage.ShouldBe(_localizer.Format("TimeReports_Summary", 2, 12m.ToString("0.##", CultureInfo.CurrentCulture), 42.5m.ToString("0.#", CultureInfo.CurrentCulture)));
    }

    [Fact]
    public async Task OnlyTheSignedInUsersReportsAreListed()
    {
        await AddReportAsync(_acme);
        await _fixture.Create.HandleAsync(TimeReportsFixture.ValidCommand(Guid.NewGuid(), _acme.Id), Ct);
        var list = List();

        await list.LoadCommand.ExecuteAsync(null);

        list.Reports.Count.ShouldBe(1);
    }

    [Fact]
    public async Task PickingAClientInTheFilterReloadsOnlyThatClientsReports()
    {
        await AddReportAsync(_acme);
        await AddReportAsync(_beta);
        await AddReportAsync(_beta, new DateOnly(2026, 10, 28));
        var list = List();
        await list.LoadCommand.ExecuteAsync(null);
        list.Reports.Count.ShouldBe(3);

        list.SelectedFilter = list.FilterClients.Single(c => c.Id == _beta.Id);

        await WaitUntilAsync(() => list.Reports.Count == 2);
        list.Reports.ShouldAllBe(r => r.ClientId == _beta.Id);
        list.SelectedFilter.Id.ShouldBe(_beta.Id);
    }

    [Fact]
    public async Task OnAClientPageOnlyThatClientsReportsAreShown()
    {
        await AddReportAsync(_acme);
        await AddReportAsync(_beta);
        var list = List(_acme.Id);

        await list.LoadCommand.ExecuteAsync(null);

        list.IsPageLayout.ShouldBeFalse();
        list.Reports.Single().ClientId.ShouldBe(_acme.Id);
    }

    [Fact]
    public async Task NewNeedsAClientAndAWorkTimeType()
    {
        var empty = new TimeReportsFixture();
        try
        {
            var noClients = new TimeReportsViewModel(
                null, new FakeCurrentUser(_user), empty.List, empty.Create, empty.Update, empty.Delete, empty.Export, empty.ExportKilometers,
                _saver, empty.ListTypes, empty.Clients, _localizer, empty.Clock);
            await noClients.LoadCommand.ExecuteAsync(null);

            noClients.NewCommand.Execute(null);

            noClients.StatusMessage.ShouldBe(_localizer.Get("TimeReports_NoClients"));
            noClients.Editor.ShouldBeNull();
        }
        finally
        {
            empty.Dispose();
        }

        var list = List();
        list.NewCommand.Execute(null);
        list.StatusMessage.ShouldBe(_localizer.Get("TimeReports_NoClients"));

        await list.LoadCommand.ExecuteAsync(null);
        list.NewCommand.Execute(null);
        list.Editor.ShouldNotBeNull();
        list.ShowEmptyState.ShouldBeFalse();
    }

    [Fact]
    public async Task SelectingAReportOpensItAndCancelClosesTheEditor()
    {
        var report = await AddReportAsync(_acme);
        var list = List();
        await list.LoadCommand.ExecuteAsync(null);

        list.SelectedReport = list.Reports.Single();
        list.Editor.ShouldNotBeNull().Location.ShouldBe(report.Location);

        list.Editor.CancelCommand.Execute(null);

        list.Editor.ShouldBeNull();
        list.SelectedReport.ShouldBeNull();
        list.ShowEmptyState.ShouldBeTrue();
    }

    [Fact]
    public async Task CopyOpensANewEditorFromTheSelectedReportWithNothingSelected()
    {
        await AddReportAsync(_acme);
        var list = List();
        await list.LoadCommand.ExecuteAsync(null);
        list.CopyCommand.CanExecute(null).ShouldBeFalse();
        list.SelectedReport = list.Reports.Single();
        list.Editor!.CancelCommand.Execute(null);
        list.SelectedReport = list.Reports.Single();
        list.CopyCommand.CanExecute(null).ShouldBeTrue();

        list.CopyCommand.Execute(null);

        list.SelectedReport.ShouldBeNull();
        list.Editor.ShouldNotBeNull().Date.ShouldBe(new DateTime(2026, 10, 31));
    }

    [Fact]
    public async Task SavingFromTheEditorReloadsTheListAndConfirms()
    {
        var list = List();
        await list.LoadCommand.ExecuteAsync(null);
        list.NewCommand.Execute(null);
        var editor = list.Editor!;
        editor.SelectedClient = editor.Clients.First(c => c.Id == _acme.Id);
        editor.Location = "Stockholm";
        editor.TotalHours = 6m;
        editor.SelectedWorkTimeType ??= editor.WorkTimeTypes[0];
        editor.StartTime = new TimeSpan(8, 0, 0);
        editor.EndTime = new TimeSpan(14, 0, 0);
        editor.DrivingKilometers ??= 0m;

        await editor.SaveCommand.ExecuteAsync(null);

        await WaitUntilAsync(() => list.StatusMessage == _localizer.Get("TimeReports_Saved"));
        list.Reports.Count.ShouldBe(1);
        list.Editor.ShouldBeNull();
    }

    [Fact]
    public async Task DeleteNeedsConfirmationAndCanBeWithdrawn()
    {
        await AddReportAsync(_acme);
        var list = List();
        await list.LoadCommand.ExecuteAsync(null);
        list.RequestDeleteCommand.CanExecute(null).ShouldBeFalse();
        list.SelectedReport = list.Reports.Single();

        list.RequestDeleteCommand.Execute(null);
        list.IsConfirmingDelete.ShouldBeTrue();
        list.CancelDeleteCommand.Execute(null);
        list.IsConfirmingDelete.ShouldBeFalse();

        list.RequestDeleteCommand.Execute(null);
        await list.ConfirmDeleteCommand.ExecuteAsync(null);

        list.Reports.ShouldBeEmpty();
        list.Editor.ShouldBeNull();
    }

    [Fact]
    public async Task DeletingAReportThatIsAlreadyGoneShowsTheError()
    {
        var report = await AddReportAsync(_acme);
        var list = List();
        await list.LoadCommand.ExecuteAsync(null);
        list.SelectedReport = list.Reports.Single();
        await _fixture.Delete.HandleAsync(new DeleteTimeReport.Command(report.Id, _user.Id), Ct);

        await list.ConfirmDeleteCommand.ExecuteAsync(null);

        list.StatusMessage.ShouldBe(Error("time_report_not_found"));
    }

    [Fact]
    public async Task ManyReportsArePagedAndTheLastPageStepsBackWhenItsOnlyRowIsDeleted()
    {
        for (var i = 0; i < 16; i++)
        {
            await AddReportAsync(_acme, new DateOnly(2026, 1, 1).AddDays(i));
        }

        var list = List();
        await list.LoadCommand.ExecuteAsync(null);
        list.PageCount.ShouldBe(2);
        list.ShowPager.ShouldBeTrue();
        list.Reports.Count.ShouldBe(15);
        list.PreviousPageCommand.CanExecute(null).ShouldBeFalse();
        list.PageLabel.ShouldBe(_localizer.Format("TimeReports_PageOf", 1, 2));

        await list.NextPageCommand.ExecuteAsync(null);
        list.CurrentPage.ShouldBe(2);
        list.Reports.Count.ShouldBe(1);
        list.NextPageCommand.CanExecute(null).ShouldBeFalse();
        list.PreviousPageCommand.CanExecute(null).ShouldBeTrue();

        list.SelectedReport = list.Reports.Single();
        await list.ConfirmDeleteCommand.ExecuteAsync(null);

        list.CurrentPage.ShouldBe(1);
        list.PageCount.ShouldBe(1);
        list.Reports.Count.ShouldBe(15);
    }

    [Fact]
    public async Task GoingBackAPageLoadsIt()
    {
        for (var i = 0; i < 16; i++)
        {
            await AddReportAsync(_acme, new DateOnly(2026, 1, 1).AddDays(i));
        }

        var list = List();
        await list.LoadCommand.ExecuteAsync(null);
        await list.NextPageCommand.ExecuteAsync(null);

        await list.PreviousPageCommand.ExecuteAsync(null);

        list.CurrentPage.ShouldBe(1);
        list.Reports.Count.ShouldBe(15);
    }

    // ---- export ----

    [Fact]
    public async Task ExportingNeedsAClientAndAValidYear()
    {
        await AddReportAsync(_acme);
        var list = List();
        await list.LoadCommand.ExecuteAsync(null);

        await list.ExportCommand.ExecuteAsync(null);
        list.StatusMessage.ShouldBe(_localizer.Get("TimeReports_ExportSelectClient"));

        list.SelectedFilter = list.FilterClients.Single(c => c.Id == _acme.Id);
        await WaitUntilAsync(() => list.Reports.Count == 1);
        list.ExportYear = null;
        await list.ExportCommand.ExecuteAsync(null);
        list.StatusMessage.ShouldBe(Error("time_report_year_invalid"));

        list.ExportYear = 1m;
        await list.ExportCommand.ExecuteAsync(null);
        list.StatusMessage.ShouldBe(Error("time_report_year_invalid"));
        _saver.Calls.ShouldBe(0);
    }

    [Fact]
    public async Task ExportingTheFilteredClientSavesAWorkbookAndReportsTheCount()
    {
        await AddReportAsync(_acme);
        var list = List();
        await list.LoadCommand.ExecuteAsync(null);
        list.SelectedFilter = list.FilterClients.Single(c => c.Id == _acme.Id);
        await WaitUntilAsync(() => list.Reports.Count == 1);
        _saver.Path = @"C:\out\acme.xlsx";

        await list.ExportCommand.ExecuteAsync(null);

        _saver.LastName.ShouldNotBeNull().ShouldEndWith(".xlsx");
        _saver.LastContent.ShouldNotBeNull().Length.ShouldBeGreaterThan(0);
        list.StatusMessage.ShouldBe(_localizer.Format("TimeReports_Exported", 1, @"C:\out\acme.xlsx"));
    }

    [Fact]
    public async Task OnAClientPageExportNeedsNoFilter()
    {
        await AddReportAsync(_acme);
        var list = List(_acme.Id);
        await list.LoadCommand.ExecuteAsync(null);
        _saver.Path = "acme.xlsx";

        await list.ExportCommand.ExecuteAsync(null);

        list.StatusMessage.ShouldBe(_localizer.Format("TimeReports_Exported", 1, "acme.xlsx"));
    }

    [Fact]
    public async Task AnExportWithNoReportsShowsTheHandlersErrorAndSkipsTheDialog()
    {
        var list = List(_acme.Id);
        await list.LoadCommand.ExecuteAsync(null);

        await list.ExportCommand.ExecuteAsync(null);

        list.StatusMessage.ShouldBe(Error("time_report_export_empty"));
        _saver.Calls.ShouldBe(0);
    }

    [Fact]
    public async Task CancellingTheSaveDialogLeavesTheStatusAlone()
    {
        await AddReportAsync(_acme);
        var list = List(_acme.Id);
        await list.LoadCommand.ExecuteAsync(null);
        var before = list.StatusMessage;
        _saver.Path = null;

        await list.ExportCommand.ExecuteAsync(null);

        list.StatusMessage.ShouldBe(before);
        _saver.Calls.ShouldBe(1);
    }

    [Fact]
    public async Task TheKilometerSummaryCoversAllClientsEvenWithAFilter()
    {
        await AddReportAsync(_acme);
        await AddReportAsync(_beta, new DateOnly(2026, 9, 1));
        var list = List();
        await list.LoadCommand.ExecuteAsync(null);
        list.SelectedFilter = list.FilterClients.Single(c => c.Id == _acme.Id);
        await WaitUntilAsync(() => list.Reports.Count == 1);
        _saver.Path = "km.xlsx";

        await list.ExportKilometersCommand.ExecuteAsync(null);

        list.StatusMessage.ShouldBe(_localizer.Format("TimeReports_Exported", 2, "km.xlsx"));
    }

    [Fact]
    public async Task TheKilometerSummaryRejectsABadYear()
    {
        var list = List();
        list.ExportYear = null;

        await list.ExportKilometersCommand.ExecuteAsync(null);

        list.StatusMessage.ShouldBe(Error("time_report_year_invalid"));
        _saver.Calls.ShouldBe(0);
    }

    [Fact]
    public void TheExportYearDefaultsToTheCurrentYear() => List().ExportYear.ShouldBe(2026m);

    // ---- email ----

    [Fact]
    public async Task EmailingDoesNothingWithoutTheEmailModule()
    {
        var list = List(_acme.Id);
        await list.LoadCommand.ExecuteAsync(null);

        await list.EmailExportCommand.ExecuteAsync(null);

        list.Mail.ShouldBeNull();
    }

    [Fact]
    public async Task EmailingNeedsAClientAndAValidYear()
    {
        var list = List(mail: new FakeMailSender());
        list.CanEmail.ShouldBeTrue();
        await list.LoadCommand.ExecuteAsync(null);

        await list.EmailExportCommand.ExecuteAsync(null);
        list.StatusMessage.ShouldBe(_localizer.Get("TimeReports_ExportSelectClient"));

        var client = List(_acme.Id, mail: new FakeMailSender());
        await client.LoadCommand.ExecuteAsync(null);
        client.ExportYear = null;
        await client.EmailExportCommand.ExecuteAsync(null);
        client.StatusMessage.ShouldBe(Error("time_report_year_invalid"));
        client.Mail.ShouldBeNull();

        var signedOut = List(signedIn: false, mail: new FakeMailSender());
        await signedOut.EmailExportCommand.ExecuteAsync(null);
        signedOut.StatusMessage.ShouldBe(_localizer.Get("TimeReports_NotSignedIn"));
    }

    [Fact]
    public async Task EmailingOpensTheMailFormInPlaceOfTheEditorAndClosingItReturns()
    {
        await AddReportAsync(_acme);
        var list = List(_acme.Id, mail: new FakeMailSender());
        await list.LoadCommand.ExecuteAsync(null);
        list.SelectedReport = list.Reports.Single();
        list.Editor.ShouldNotBeNull();

        await list.EmailExportCommand.ExecuteAsync(null);

        list.Mail.ShouldNotBeNull();
        list.Editor.ShouldBeNull();
        list.ShowEmptyState.ShouldBeFalse();

        list.Mail.CancelCommand.Execute(null);

        list.Mail.ShouldBeNull();
        list.ShowEmptyState.ShouldBeTrue();
    }

    [Fact]
    public async Task OpeningAReportWhileTheMailFormIsOpenClosesTheForm()
    {
        await AddReportAsync(_acme);
        var list = List(_acme.Id, mail: new FakeMailSender());
        await list.LoadCommand.ExecuteAsync(null);
        await list.EmailExportCommand.ExecuteAsync(null);
        list.Mail.ShouldNotBeNull();

        list.SelectedReport = list.Reports.Single();

        list.Mail.ShouldBeNull();
        list.Editor.ShouldNotBeNull();
    }

    private sealed class FakeCurrentUser(AuthenticatedUser? user) : ICurrentUser
    {
        public AuthenticatedUser? User { get; } = user;
    }

    private sealed class FakeMailSender : ISystemMailSender
    {
        public Task<Result<int>> SendAsync(SystemMail mail, CancellationToken cancellationToken) => Task.FromResult<Result<int>>(1);
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
