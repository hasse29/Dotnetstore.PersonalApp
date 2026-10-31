using Dotnetstore.PersonalApp.Modules.TimeReports.Features.TimeReports;
using Dotnetstore.PersonalApp.Shared.Localization;

namespace Dotnetstore.PersonalApp.Modules.TimeReports.Tests;

public sealed class TimeReportEditorViewModelTests : IDisposable
{
    private readonly TimeReportsFixture _fixture = new();

    public void Dispose() => _fixture.Dispose();

    [Fact]
    public async Task ACopyKeepsTheValuesMovesTheDateAndSavesAsANewReport()
    {
        var ct = TestContext.Current.CancellationToken;
        var user = Guid.NewGuid();
        var client = _fixture.Clients.Add("Acme AB");
        var original = (await _fixture.Create.HandleAsync(TimeReportsFixture.ValidCommand(user, client.Id), ct)).Value;
        var types = (await _fixture.ListTypes.HandleAsync(new Features.WorkTimeTypes.ListWorkTimeTypes.Query(), ct)).Items;
        var editor = new TimeReportEditorViewModel(
            original, true, user, null, [client], types, new DateOnly(2026, 10, 31), _fixture.Create, _fixture.Update, new Localizer());

        Assert.Equal(original.Date.AddDays(1).ToDateTime(TimeOnly.MinValue), editor.Date);
        Assert.Equal(original.TotalHours, editor.TotalHours);
        Assert.Equal(original.Location, editor.Location);

        await editor.SaveCommand.ExecuteAsync(null);

        var all = await _fixture.List.HandleAsync(new ListTimeReports.Query(user), ct);
        Assert.Equal(2, all.TotalCount);
    }
}