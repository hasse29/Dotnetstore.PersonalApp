using Dotnetstore.PersonalApp.Modules.TimeReports.Features.WorkTimeTypes;

namespace Dotnetstore.PersonalApp.Modules.TimeReports.Tests;

public sealed class WorkTimeTypeHandlerTests : IDisposable
{
    private readonly TimeReportsFixture _fixture = new();

    public void Dispose() => _fixture.Dispose();

    [Fact]
    public async Task SeedsWorkSickVacationAndParentalLeave()
    {
        var response = await _fixture.ListTypes.HandleAsync(new ListWorkTimeTypes.Query(), TestContext.Current.CancellationToken);

        Assert.Equal(["Parental leave", "Sick", "Vacation", "Work"], response.Items.Select(t => t.Name));
    }

    [Fact]
    public async Task CreatesAType()
    {
        var result = await _fixture.CreateType.HandleAsync(new CreateWorkTimeType.Command(" Training ", "Courses"), TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.Equal("Training", result.Value.Name);
    }

    [Fact]
    public async Task RejectsADuplicateNameIgnoringCase()
    {
        var result = await _fixture.CreateType.HandleAsync(new CreateWorkTimeType.Command("sick", null), TestContext.Current.CancellationToken);

        Assert.Equal("work_time_type_conflict", result.Error.Code);
    }

    [Fact]
    public async Task RejectsAnEmptyName()
    {
        var result = await _fixture.CreateType.HandleAsync(new CreateWorkTimeType.Command(" ", null), TestContext.Current.CancellationToken);

        Assert.Equal("work_time_type_name_required", result.Error.Code);
    }

    [Fact]
    public async Task RenamesAType()
    {
        var ct = TestContext.Current.CancellationToken;
        var sick = (await _fixture.ListTypes.HandleAsync(new ListWorkTimeTypes.Query("Sick"), ct)).Items.Single();

        var result = await _fixture.UpdateType.HandleAsync(new UpdateWorkTimeType.Command(sick.Id, "Ill", null), ct);

        Assert.Equal("Ill", result.Value.Name);
    }

    [Fact]
    public async Task ATypeInUseCannotBeDeleted()
    {
        var ct = TestContext.Current.CancellationToken;
        var client = _fixture.Clients.Add("Acme AB");
        await _fixture.Create.HandleAsync(TimeReportsFixture.ValidCommand(Guid.NewGuid(), client.Id), ct);

        var result = await _fixture.DeleteType.HandleAsync(new DeleteWorkTimeType.Command(Persistence.WorkTimeTypeSeed.WorkId), ct);

        Assert.Equal("work_time_type_in_use", result.Error.Code);
    }

    [Fact]
    public async Task AnUnusedTypeCanBeDeleted()
    {
        var ct = TestContext.Current.CancellationToken;
        var sick = (await _fixture.ListTypes.HandleAsync(new ListWorkTimeTypes.Query("Sick"), ct)).Items.Single();

        var result = await _fixture.DeleteType.HandleAsync(new DeleteWorkTimeType.Command(sick.Id), ct);

        Assert.True(result.IsSuccess);
    }
}