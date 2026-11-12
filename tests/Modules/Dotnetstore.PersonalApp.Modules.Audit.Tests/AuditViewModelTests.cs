using Dotnetstore.PersonalApp.Modules.Audit.Features.Entries;
using Dotnetstore.PersonalApp.Modules.Audit.Resources;
using Dotnetstore.PersonalApp.Shared.Audit;
using Dotnetstore.PersonalApp.Shared.Localization;

namespace Dotnetstore.PersonalApp.Modules.Audit.Tests;

public sealed class AuditViewModelTests
{
    private static async Task<(AuditViewModel ViewModel, AuditFixture Fixture)> NewAsync()
    {
        var fixture = new AuditFixture();
        var writer = new AuditWriter(fixture.DbFactory);
        var at = new DateTimeOffset(2026, 11, 12, 9, 0, 0, TimeSpan.Zero);
        writer.Start();
        writer.Record(new AuditEntry(at, null, "anna", "Clients.CreateClient", AuditKind.Write, Guid.NewGuid(), true, null, ["Name"], 3));
        writer.Record(new AuditEntry(at.AddMinutes(1), null, "anna", "Clients.DeleteClient", AuditKind.Write, null, false, "client_not_found", [], 2));
        writer.Record(new AuditEntry(at.AddMinutes(2), null, "bo", "Clients.ListClients", AuditKind.Read, null, true, null, [], 1));
        await writer.DisposeAsync();

        var localizer = new Localizer();
        localizer.AddResources(AuditStrings.ResourceManager);
        localizer.SetCulture("en");
        return (new AuditViewModel(new ListAuditEntries.Handler(fixture.DbFactory), localizer), fixture);
    }

    [Fact]
    public async Task LoadsTheNewestEntriesFirstAndCountsThem()
    {
        var (viewModel, fixture) = await NewAsync();
        await using var _ = fixture;

        await viewModel.LoadCommand.ExecuteAsync(null);

        viewModel.Entries.Select(e => e.Operation).ShouldBe(["Clients.ListClients", "Clients.DeleteClient", "Clients.CreateClient"]);
        viewModel.StatusMessage.ShouldBe("3 entries");
    }

    [Fact]
    public async Task TheFiltersNarrowTheList()
    {
        var (viewModel, fixture) = await NewAsync();
        await using var _ = fixture;

        viewModel.OnlyChanges = true;
        await viewModel.LoadCommand.ExecuteAsync(null);
        viewModel.Entries.Count.ShouldBe(2);

        viewModel.OnlyFailed = true;
        await viewModel.LoadCommand.ExecuteAsync(null);
        viewModel.Entries.Select(e => e.Outcome).ShouldBe(["client_not_found"]);

        viewModel.OnlyChanges = false;
        viewModel.OnlyFailed = false;
        viewModel.Search = "bo";
        await viewModel.LoadCommand.ExecuteAsync(null);
        viewModel.Entries.Select(e => e.User).ShouldBe(["bo"]);
    }
}
