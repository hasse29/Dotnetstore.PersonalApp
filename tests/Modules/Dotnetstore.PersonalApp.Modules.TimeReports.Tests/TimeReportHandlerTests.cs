using Dotnetstore.PersonalApp.Modules.TimeReports.Features.TimeReports;
using Dotnetstore.PersonalApp.Modules.TimeReports.Features.WorkTimeTypes;

namespace Dotnetstore.PersonalApp.Modules.TimeReports.Tests;

public sealed class TimeReportHandlerTests : IDisposable
{
    private readonly TimeReportsFixture _fixture = new();
    private readonly Guid _user = Guid.NewGuid();

    public void Dispose() => _fixture.Dispose();

    [Fact]
    public async Task CreatesAReportWithTheClientName()
    {
        var client = _fixture.Clients.Add("Acme AB");

        var result = await _fixture.Create.HandleAsync(TimeReportsFixture.ValidCommand(_user, client.Id), TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.Equal("Acme AB", result.Value.ClientName);
        Assert.Equal(8m, result.Value.TotalHours);
        Assert.Equal(42.5m, result.Value.DrivingKilometers);
        Assert.Equal("08:00–16:30", result.Value.TimeRange);
    }

    [Fact]
    public async Task TotalHoursMayDifferFromTheStartAndEndTimes()
    {
        var client = _fixture.Clients.Add("Acme AB");
        var command = TimeReportsFixture.ValidCommand(_user, client.Id) with { TotalHours = 5m };

        var result = await _fixture.Create.HandleAsync(command, TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.Equal(5m, result.Value.TotalHours);
    }

    [Fact]
    public async Task RejectsAnEndTimeNotAfterTheStartTime()
    {
        var client = _fixture.Clients.Add("Acme AB");
        var command = TimeReportsFixture.ValidCommand(_user, client.Id) with { EndTime = new TimeOnly(8, 0) };

        var result = await _fixture.Create.HandleAsync(command, TestContext.Current.CancellationToken);

        Assert.Equal("time_report_end_before_start", result.Error.Code);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(24.5)]
    public async Task RejectsTotalHoursOutsideTheRange(double hours)
    {
        var client = _fixture.Clients.Add("Acme AB");
        var command = TimeReportsFixture.ValidCommand(_user, client.Id) with { TotalHours = (decimal)hours };

        var result = await _fixture.Create.HandleAsync(command, TestContext.Current.CancellationToken);

        Assert.Equal("time_report_total_hours_invalid", result.Error.Code);
    }

    [Fact]
    public async Task RejectsNegativeDriving()
    {
        var client = _fixture.Clients.Add("Acme AB");
        var command = TimeReportsFixture.ValidCommand(_user, client.Id) with { DrivingKilometers = -1m };

        var result = await _fixture.Create.HandleAsync(command, TestContext.Current.CancellationToken);

        Assert.Equal("time_report_driving_invalid", result.Error.Code);
    }

    [Fact]
    public async Task ReportsCarryTheirWorkTimeType()
    {
        var client = _fixture.Clients.Add("Acme AB");
        var ct = TestContext.Current.CancellationToken;
        var sick = (await _fixture.ListTypes.HandleAsync(new ListWorkTimeTypes.Query("Sick"), ct)).Items.Single();
        var command = TimeReportsFixture.ValidCommand(_user, client.Id) with { WorkTimeTypeId = sick.Id };

        var result = await _fixture.Create.HandleAsync(command, ct);

        Assert.Equal("Sick", result.Value.WorkTimeTypeName);
        Assert.Equal("Sick", Assert.Single((await _fixture.List.HandleAsync(new ListTimeReports.Query(_user), ct)).Items).WorkTimeTypeName);
    }

    [Fact]
    public async Task RejectsAnUnknownWorkTimeType()
    {
        var client = _fixture.Clients.Add("Acme AB");
        var command = TimeReportsFixture.ValidCommand(_user, client.Id) with { WorkTimeTypeId = Guid.NewGuid() };

        var result = await _fixture.Create.HandleAsync(command, TestContext.Current.CancellationToken);

        Assert.Equal("time_report_work_time_type_not_found", result.Error.Code);
    }

    [Fact]
    public async Task RejectsAnUnknownClient()
    {
        var result = await _fixture.Create.HandleAsync(TimeReportsFixture.ValidCommand(_user, Guid.NewGuid()), TestContext.Current.CancellationToken);

        Assert.Equal("time_report_client_not_found", result.Error.Code);
    }

    [Fact]
    public async Task ListShowsOnlyTheUsersOwnReportsWithTotals()
    {
        var acme = _fixture.Clients.Add("Acme AB");
        var other = _fixture.Clients.Add("Other AB");
        var ct = TestContext.Current.CancellationToken;
        await _fixture.Create.HandleAsync(TimeReportsFixture.ValidCommand(_user, acme.Id), ct);
        await _fixture.Create.HandleAsync(TimeReportsFixture.ValidCommand(_user, other.Id) with { TotalHours = 2m, DrivingKilometers = 10m }, ct);
        await _fixture.Create.HandleAsync(TimeReportsFixture.ValidCommand(Guid.NewGuid(), acme.Id), ct);

        var all = await _fixture.List.HandleAsync(new ListTimeReports.Query(_user), ct);
        var forAcme = await _fixture.List.HandleAsync(new ListTimeReports.Query(_user, acme.Id), ct);

        Assert.Equal(2, all.TotalCount);
        Assert.Equal(10m, all.TotalHours);
        Assert.Equal(52.5m, all.TotalKilometers);
        Assert.Single(forAcme.Items);
        Assert.Equal(8m, forAcme.TotalHours);
    }

    [Fact]
    public async Task UpdateChangesTheReportButOnlyForItsOwner()
    {
        var client = _fixture.Clients.Add("Acme AB");
        var ct = TestContext.Current.CancellationToken;
        var created = (await _fixture.Create.HandleAsync(TimeReportsFixture.ValidCommand(_user, client.Id), ct)).Value;
        var update = new UpdateTimeReport.Command(
            created.Id, _user, client.Id, created.WorkTimeTypeId, created.Date, created.StartTime, created.EndTime, 7.5m, "Remote", 0m);

        var byOwner = await _fixture.Update.HandleAsync(update, ct);
        var byOther = await _fixture.Update.HandleAsync(update with { UserId = Guid.NewGuid() }, ct);

        Assert.Equal(7.5m, byOwner.Value.TotalHours);
        Assert.Equal("Remote", byOwner.Value.Location);
        Assert.Equal("time_report_not_found", byOther.Error.Code);
    }

    [Fact]
    public async Task DeleteRemovesTheReportButOnlyForItsOwner()
    {
        var client = _fixture.Clients.Add("Acme AB");
        var ct = TestContext.Current.CancellationToken;
        var created = (await _fixture.Create.HandleAsync(TimeReportsFixture.ValidCommand(_user, client.Id), ct)).Value;

        var byOther = await _fixture.Delete.HandleAsync(new DeleteTimeReport.Command(created.Id, Guid.NewGuid()), ct);
        var byOwner = await _fixture.Delete.HandleAsync(new DeleteTimeReport.Command(created.Id, _user), ct);

        Assert.False(byOther.IsSuccess);
        Assert.True(byOwner.IsSuccess);
        Assert.Equal(0, (await _fixture.List.HandleAsync(new ListTimeReports.Query(_user), ct)).TotalCount);
    }

    [Fact]
    public async Task ReportsOfADeletedClientAreKeptWithoutAName()
    {
        var ct = TestContext.Current.CancellationToken;
        var client = _fixture.Clients.Add("Gone AB");
        await _fixture.Create.HandleAsync(TimeReportsFixture.ValidCommand(_user, client.Id), ct);
        var emptyLookup = new TimeReportsFixture.FakeClients();
        var list = new ListTimeReports.Handler(_fixture.DbFactory, emptyLookup);

        var response = await list.HandleAsync(new ListTimeReports.Query(_user), ct);

        Assert.Null(Assert.Single(response.Items).ClientName);
    }
}