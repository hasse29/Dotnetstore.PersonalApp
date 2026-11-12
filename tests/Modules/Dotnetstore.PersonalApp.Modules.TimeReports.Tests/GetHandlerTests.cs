using Dotnetstore.PersonalApp.Modules.TimeReports.Features.TimeReports;
using Dotnetstore.PersonalApp.Modules.TimeReports.Features.WorkTimeTypes;

namespace Dotnetstore.PersonalApp.Modules.TimeReports.Tests;

public sealed class GetHandlerTests : IDisposable
{
    private readonly TimeReportsFixture _fixture = new();

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public void Dispose() => _fixture.Dispose();

    [Fact]
    public async Task GetsATimeReportWithItsClientAndTypeNames()
    {
        var user = Guid.NewGuid();
        var client = _fixture.Clients.Add("Acme");
        var created = await _fixture.Create.HandleAsync(TimeReportsFixture.ValidCommand(user, client.Id), Ct);

        var result = await new GetTimeReport.Handler(_fixture.DbFactory, _fixture.Clients).HandleAsync(new GetTimeReport.Query(created.Value.Id, user), Ct);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ClientName.ShouldBe("Acme");
        result.Value.WorkTimeTypeName.ShouldNotBeNullOrEmpty();
        result.Value.TotalHours.ShouldBe(8m);
    }

    [Fact]
    public async Task AUserCannotGetSomeoneElsesTimeReport()
    {
        var client = _fixture.Clients.Add("Acme");
        var created = await _fixture.Create.HandleAsync(TimeReportsFixture.ValidCommand(Guid.NewGuid(), client.Id), Ct);

        var result = await new GetTimeReport.Handler(_fixture.DbFactory, _fixture.Clients).HandleAsync(new GetTimeReport.Query(created.Value.Id, Guid.NewGuid()), Ct);

        result.Error.Code.ShouldBe("time_report_not_found");
    }

    [Fact]
    public async Task GetsAWorkTimeTypeById()
    {
        var created = await _fixture.CreateType.HandleAsync(new CreateWorkTimeType.Command("Training", "Courses"), Ct);

        var result = await new GetWorkTimeType.Handler(_fixture.DbFactory).HandleAsync(new GetWorkTimeType.Query(created.Value.Id), Ct);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Name.ShouldBe("Training");
    }

    [Fact]
    public async Task AnUnknownWorkTimeTypeIsNotFound()
    {
        var result = await new GetWorkTimeType.Handler(_fixture.DbFactory).HandleAsync(new GetWorkTimeType.Query(Guid.NewGuid()), Ct);

        result.Error.Code.ShouldBe("work_time_type_not_found");
    }
}
