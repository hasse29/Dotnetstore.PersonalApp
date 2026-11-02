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

        result.IsSuccess.ShouldBeTrue();
        result.Value.UserId.ShouldBe(_user);
        result.Value.Title.ShouldBe("Backend developer");
        result.Value.TargetJob.ShouldBeNull();
        result.Value.Summary.ShouldBe("Summary");
    }

    [Theory]
    [InlineData("", "cv_title_required")]
    [InlineData("   ", "cv_title_required")]
    public async Task CreateRequiresATitle(string title, string expectedCode)
    {
        var result = await _fixture.Create.HandleAsync(CvsFixture.ValidCv(_user, title), Ct);

        result.Error.Code.ShouldBe(expectedCode);
    }

    [Fact]
    public async Task CreateRejectsTooLongFieldsAndMissingUser()
    {
        var longTitle = await _fixture.Create.HandleAsync(CvsFixture.ValidCv(_user, new string('a', 201)), Ct);
        var longJob = await _fixture.Create.HandleAsync(CvsFixture.ValidCv(_user) with { TargetJob = new string('a', 201) }, Ct);
        var longSummary = await _fixture.Create.HandleAsync(CvsFixture.ValidCv(_user) with { Summary = new string('a', 4001) }, Ct);
        var noUser = await _fixture.Create.HandleAsync(CvsFixture.ValidCv(Guid.Empty), Ct);

        longTitle.Error.Code.ShouldBe("cv_title_too_long");
        longJob.Error.Code.ShouldBe("cv_target_job_too_long");
        longSummary.Error.Code.ShouldBe("cv_summary_too_long");
        noUser.Error.Code.ShouldBe("cv_user_required");
    }

    [Fact]
    public async Task UserCanHaveSeveralCvsButTitlesAreUniquePerUserIgnoringCase()
    {
        await _fixture.Create.HandleAsync(CvsFixture.ValidCv(_user, "Backend"), Ct);
        var other = await _fixture.Create.HandleAsync(CvsFixture.ValidCv(_user, "Frontend"), Ct);
        var duplicate = await _fixture.Create.HandleAsync(CvsFixture.ValidCv(_user, "backend"), Ct);
        var otherUser = await _fixture.Create.HandleAsync(CvsFixture.ValidCv(Guid.NewGuid(), "Backend"), Ct);

        other.IsSuccess.ShouldBeTrue();
        duplicate.Error.Code.ShouldBe("cv_conflict");
        otherUser.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public async Task ListReturnsOnlyTheUsersCvsOrderedByTitle()
    {
        await _fixture.Create.HandleAsync(CvsFixture.ValidCv(_user, "Zebra"), Ct);
        await _fixture.Create.HandleAsync(CvsFixture.ValidCv(_user, "alpha"), Ct);
        await _fixture.Create.HandleAsync(CvsFixture.ValidCv(Guid.NewGuid(), "Someone else's"), Ct);

        var response = await _fixture.List.HandleAsync(new ListCvs.Query(_user), Ct);

        response.TotalCount.ShouldBe(2);
        response.Items.Select(c => c.Title).ShouldBe(["alpha", "Zebra"]);
    }

    [Fact]
    public async Task UpdateChangesTheCvAndKeepsTitlesUnique()
    {
        var first = (await _fixture.Create.HandleAsync(CvsFixture.ValidCv(_user, "One"), Ct)).Value;
        await _fixture.Create.HandleAsync(CvsFixture.ValidCv(_user, "Two"), Ct);

        var renamed = await _fixture.Update.HandleAsync(new UpdateCv.Command(first.Id, _user, "ONE", "Job", null), Ct);
        var clash = await _fixture.Update.HandleAsync(new UpdateCv.Command(first.Id, _user, "two", "Job", null), Ct);

        renamed.IsSuccess.ShouldBeTrue();
        renamed.Value.Title.ShouldBe("ONE");
        renamed.Value.Summary.ShouldBeNull();
        clash.Error.Code.ShouldBe("cv_conflict");
    }

    [Fact]
    public async Task OtherUsersCannotReadChangeOrDeleteACv()
    {
        var cv = (await _fixture.Create.HandleAsync(CvsFixture.ValidCv(_user), Ct)).Value;
        var intruder = Guid.NewGuid();

        var get = await _fixture.Get.HandleAsync(new GetCv.Query(cv.Id, intruder), Ct);
        var update = await _fixture.Update.HandleAsync(new UpdateCv.Command(cv.Id, intruder, "Mine now", null, null), Ct);
        var delete = await _fixture.Delete.HandleAsync(new DeleteCv.Command(cv.Id, intruder), Ct);

        get.Error.Code.ShouldBe("cv_not_found");
        update.Error.Code.ShouldBe("cv_not_found");
        delete.Error.Code.ShouldBe("cv_not_found");
        (await _fixture.Get.HandleAsync(new GetCv.Query(cv.Id, _user), Ct)).IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public async Task DeleteRemovesTheCv()
    {
        var cv = (await _fixture.Create.HandleAsync(CvsFixture.ValidCv(_user), Ct)).Value;

        var result = await _fixture.Delete.HandleAsync(new DeleteCv.Command(cv.Id, _user), Ct);
        var again = await _fixture.Delete.HandleAsync(new DeleteCv.Command(cv.Id, _user), Ct);

        result.IsSuccess.ShouldBeTrue();
        again.Error.Code.ShouldBe("cv_not_found");
    }

    [Fact]
    public async Task UserDeletedRemovesTheUsersCvsOnly()
    {
        var other = Guid.NewGuid();
        await _fixture.Create.HandleAsync(CvsFixture.ValidCv(_user, "One"), Ct);
        await _fixture.Create.HandleAsync(CvsFixture.ValidCv(_user, "Two"), Ct);
        await _fixture.Create.HandleAsync(CvsFixture.ValidCv(other), Ct);

        await new UserDeletedConsumer(_fixture.DeleteForUser).ConsumeAsync(new UserDeleted(_user), Ct);

        (await _fixture.List.HandleAsync(new ListCvs.Query(_user), Ct)).Items.ShouldBeEmpty();
        (await _fixture.List.HandleAsync(new ListCvs.Query(other), Ct)).Items.ShouldHaveSingleItem();
    }
}
