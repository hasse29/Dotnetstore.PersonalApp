using Dotnetstore.PersonalApp.Modules.Cvs.Consumers;
using Dotnetstore.PersonalApp.Modules.Cvs.Features.Cvs;
using Dotnetstore.PersonalApp.Shared.Contracts.Events;

namespace Dotnetstore.PersonalApp.Modules.Cvs.Tests;

public sealed class CvHandlerTests : IDisposable
{
    private readonly CvsFixture _fixture = new();
    private readonly Guid _user = Guid.NewGuid();

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public void Dispose() => _fixture.Dispose();

    [Fact]
    public async Task CreateStoresATrimmedCvForTheUser()
    {
        var result = await _fixture.Create.HandleAsync(new CreateCv.Command(_user, "  Backend developer ", "  ", " Summary "), Ct);

        Assert.True(result.IsSuccess);
        Assert.Equal(_user, result.Value.UserId);
        Assert.Equal("Backend developer", result.Value.Title);
        Assert.Null(result.Value.TargetJob);
        Assert.Equal("Summary", result.Value.Summary);
    }

    [Theory]
    [InlineData("", "cv_title_required")]
    [InlineData("   ", "cv_title_required")]
    public async Task CreateRequiresATitle(string title, string expectedCode)
    {
        var result = await _fixture.Create.HandleAsync(CvsFixture.ValidCv(_user, title), Ct);

        Assert.Equal(expectedCode, result.Error.Code);
    }

    [Fact]
    public async Task CreateRejectsTooLongFieldsAndMissingUser()
    {
        var longTitle = await _fixture.Create.HandleAsync(CvsFixture.ValidCv(_user, new string('a', 201)), Ct);
        var longJob = await _fixture.Create.HandleAsync(CvsFixture.ValidCv(_user) with { TargetJob = new string('a', 201) }, Ct);
        var longSummary = await _fixture.Create.HandleAsync(CvsFixture.ValidCv(_user) with { Summary = new string('a', 4001) }, Ct);
        var noUser = await _fixture.Create.HandleAsync(CvsFixture.ValidCv(Guid.Empty), Ct);

        Assert.Equal("cv_title_too_long", longTitle.Error.Code);
        Assert.Equal("cv_target_job_too_long", longJob.Error.Code);
        Assert.Equal("cv_summary_too_long", longSummary.Error.Code);
        Assert.Equal("cv_user_required", noUser.Error.Code);
    }

    [Fact]
    public async Task UserCanHaveSeveralCvsButTitlesAreUniquePerUserIgnoringCase()
    {
        await _fixture.Create.HandleAsync(CvsFixture.ValidCv(_user, "Backend"), Ct);
        var other = await _fixture.Create.HandleAsync(CvsFixture.ValidCv(_user, "Frontend"), Ct);
        var duplicate = await _fixture.Create.HandleAsync(CvsFixture.ValidCv(_user, "backend"), Ct);
        var otherUser = await _fixture.Create.HandleAsync(CvsFixture.ValidCv(Guid.NewGuid(), "Backend"), Ct);

        Assert.True(other.IsSuccess);
        Assert.Equal("cv_conflict", duplicate.Error.Code);
        Assert.True(otherUser.IsSuccess);
    }

    [Fact]
    public async Task ListReturnsOnlyTheUsersCvsOrderedByTitle()
    {
        await _fixture.Create.HandleAsync(CvsFixture.ValidCv(_user, "Zebra"), Ct);
        await _fixture.Create.HandleAsync(CvsFixture.ValidCv(_user, "alpha"), Ct);
        await _fixture.Create.HandleAsync(CvsFixture.ValidCv(Guid.NewGuid(), "Someone else's"), Ct);

        var response = await _fixture.List.HandleAsync(new ListCvs.Query(_user), Ct);

        Assert.Equal(2, response.TotalCount);
        Assert.Equal(["alpha", "Zebra"], response.Items.Select(c => c.Title));
    }

    [Fact]
    public async Task UpdateChangesTheCvAndKeepsTitlesUnique()
    {
        var first = (await _fixture.Create.HandleAsync(CvsFixture.ValidCv(_user, "One"), Ct)).Value;
        await _fixture.Create.HandleAsync(CvsFixture.ValidCv(_user, "Two"), Ct);

        var renamed = await _fixture.Update.HandleAsync(new UpdateCv.Command(first.Id, _user, "ONE", "Job", null), Ct);
        var clash = await _fixture.Update.HandleAsync(new UpdateCv.Command(first.Id, _user, "two", "Job", null), Ct);

        Assert.True(renamed.IsSuccess);
        Assert.Equal("ONE", renamed.Value.Title);
        Assert.Null(renamed.Value.Summary);
        Assert.Equal("cv_conflict", clash.Error.Code);
    }

    [Fact]
    public async Task OtherUsersCannotReadChangeOrDeleteACv()
    {
        var cv = (await _fixture.Create.HandleAsync(CvsFixture.ValidCv(_user), Ct)).Value;
        var intruder = Guid.NewGuid();

        var get = await _fixture.Get.HandleAsync(new GetCv.Query(cv.Id, intruder), Ct);
        var update = await _fixture.Update.HandleAsync(new UpdateCv.Command(cv.Id, intruder, "Mine now", null, null), Ct);
        var delete = await _fixture.Delete.HandleAsync(new DeleteCv.Command(cv.Id, intruder), Ct);

        Assert.Equal("cv_not_found", get.Error.Code);
        Assert.Equal("cv_not_found", update.Error.Code);
        Assert.Equal("cv_not_found", delete.Error.Code);
        Assert.True((await _fixture.Get.HandleAsync(new GetCv.Query(cv.Id, _user), Ct)).IsSuccess);
    }

    [Fact]
    public async Task DeleteRemovesTheCv()
    {
        var cv = (await _fixture.Create.HandleAsync(CvsFixture.ValidCv(_user), Ct)).Value;

        var result = await _fixture.Delete.HandleAsync(new DeleteCv.Command(cv.Id, _user), Ct);
        var again = await _fixture.Delete.HandleAsync(new DeleteCv.Command(cv.Id, _user), Ct);

        Assert.True(result.IsSuccess);
        Assert.Equal("cv_not_found", again.Error.Code);
    }

    [Fact]
    public async Task UserDeletedRemovesTheUsersCvsOnly()
    {
        var other = Guid.NewGuid();
        await _fixture.Create.HandleAsync(CvsFixture.ValidCv(_user, "One"), Ct);
        await _fixture.Create.HandleAsync(CvsFixture.ValidCv(_user, "Two"), Ct);
        await _fixture.Create.HandleAsync(CvsFixture.ValidCv(other), Ct);

        await new UserDeletedConsumer(_fixture.DeleteForUser).ConsumeAsync(new UserDeleted(_user), Ct);

        Assert.Empty((await _fixture.List.HandleAsync(new ListCvs.Query(_user), Ct)).Items);
        Assert.Single((await _fixture.List.HandleAsync(new ListCvs.Query(other), Ct)).Items);
    }
}
