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

        response.Items.Select(t => t.Name).ShouldBe(["Parental leave", "Sick", "Vacation", "Work"]);
    }

    [Fact]
    public async Task CreatesAType()
    {
        var result = await _fixture.CreateType.HandleAsync(new CreateWorkTimeType.Command(" Training ", "Courses"), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Name.ShouldBe("Training");
    }

    [Fact]
    public async Task RejectsADuplicateNameIgnoringCase()
    {
        var result = await _fixture.CreateType.HandleAsync(new CreateWorkTimeType.Command("sick", null), TestContext.Current.CancellationToken);

        result.Error.Code.ShouldBe("work_time_type_conflict");
    }

    [Fact]
    public async Task RejectsAnEmptyName()
    {
        var result = await _fixture.CreateType.HandleAsync(new CreateWorkTimeType.Command(" ", null), TestContext.Current.CancellationToken);

        result.Error.Code.ShouldBe("work_time_type_name_required");
    }

    [Fact]
    public async Task RenamesAType()
    {
        var ct = TestContext.Current.CancellationToken;
        var sick = (await _fixture.ListTypes.HandleAsync(new ListWorkTimeTypes.Query("Sick"), ct)).Items.Single();

        var result = await _fixture.UpdateType.HandleAsync(new UpdateWorkTimeType.Command(sick.Id, "Ill", null), ct);

        result.Value.Name.ShouldBe("Ill");
    }

    [Fact]
    public async Task ATypeInUseCannotBeDeleted()
    {
        var ct = TestContext.Current.CancellationToken;
        var client = _fixture.Clients.Add("Acme AB");
        await _fixture.Create.HandleAsync(TimeReportsFixture.ValidCommand(Guid.NewGuid(), client.Id), ct);

        var result = await _fixture.DeleteType.HandleAsync(new DeleteWorkTimeType.Command(Persistence.WorkTimeTypeSeed.WorkId), ct);

        result.Error.Code.ShouldBe("work_time_type_in_use");
    }

    [Fact]
    public async Task AnUnusedTypeCanBeDeleted()
    {
        var ct = TestContext.Current.CancellationToken;
        var sick = (await _fixture.ListTypes.HandleAsync(new ListWorkTimeTypes.Query("Sick"), ct)).Items.Single();

        var result = await _fixture.DeleteType.HandleAsync(new DeleteWorkTimeType.Command(sick.Id), ct);

        result.IsSuccess.ShouldBeTrue();
    }
}