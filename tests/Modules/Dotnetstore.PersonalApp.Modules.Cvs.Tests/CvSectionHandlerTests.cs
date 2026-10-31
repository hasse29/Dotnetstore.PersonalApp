using Dotnetstore.PersonalApp.Modules.Cvs.Domain;
using Dotnetstore.PersonalApp.Modules.Cvs.Features.Cvs;
using Dotnetstore.PersonalApp.Shared.Users;

namespace Dotnetstore.PersonalApp.Modules.Cvs.Tests;

public sealed class CvSectionHandlerTests : IDisposable
{
    private static readonly byte[] Png = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];

    private readonly CvsFixture _fixture = new();
    private readonly Guid _user = Guid.NewGuid();

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public void Dispose() => _fixture.Dispose();

    private async Task<CvDto> NewCvAsync(Guid? userId = null, string title = "Backend developer") =>
        (await _fixture.Create.HandleAsync(CvsFixture.ValidCv(userId ?? _user, title), Ct)).Value;

    [Fact]
    public async Task DetailCollectsTheUserAndEverySection()
    {
        var cv = await NewCvAsync();
        _fixture.Users.User = new UserReference(_user, "Anna", "Berg", "anna", new DateOnly(1990, 5, 17));
        await _fixture.SaveWork.HandleAsync(new SaveWorkExperience.Command(cv.Id, _user, null, " Acme ", "Developer", new DateOnly(2020, 1, 1), null, "Built things", []), Ct);
        await _fixture.SaveEducation.HandleAsync(new SaveEducation.Command(cv.Id, _user, null, "KTH", "MSc", new DateOnly(2010, 9, 1), new DateOnly(2015, 6, 1), null), Ct);
        await _fixture.SaveCourse.HandleAsync(new SaveCourse.Command(cv.Id, _user, null, "Azure", "Microsoft", 2022), Ct);
        await _fixture.SaveLanguage.HandleAsync(new SaveLanguageSkill.Command(cv.Id, _user, null, "Swedish", LanguageLevel.Native), Ct);
        await _fixture.SetLicence.HandleAsync(new SetDriverLicence.Command(cv.Id, _user, ["BE", "b"]), Ct);
        await _fixture.SetPhoto.HandleAsync(new SetCvPhoto.Command(cv.Id, _user, Png), Ct);

        var detail = (await _fixture.GetDetail.HandleAsync(new GetCvDetail.Query(cv.Id, _user), Ct)).Value;

        Assert.Equal("Anna Berg", detail.User?.FullName);
        Assert.Equal("Acme", Assert.Single(detail.WorkExperiences).Employer);
        Assert.Null(detail.WorkExperiences[0].EndDate);
        Assert.Equal("MSc", Assert.Single(detail.Educations).Programme);
        Assert.Equal(2022, Assert.Single(detail.Courses).CompletedYear);
        Assert.Equal(LanguageLevel.Native, Assert.Single(detail.Languages).Level);
        Assert.Equal(["B", "BE"], detail.DriverLicence);
        Assert.Equal(Png, detail.Photo);
    }

    [Fact]
    public async Task DetailOfSomeoneElsesCvIsNotFound()
    {
        var cv = await NewCvAsync();

        var result = await _fixture.GetDetail.HandleAsync(new GetCvDetail.Query(cv.Id, Guid.NewGuid()), Ct);

        Assert.Equal("cv_not_found", result.Error.Code);
    }

    [Fact]
    public async Task WorkExperienceCanBeChangedAndRequiresAnEndAfterTheStart()
    {
        var cv = await NewCvAsync();
        var job = (await _fixture.SaveWork.HandleAsync(
            new SaveWorkExperience.Command(cv.Id, _user, null, "Acme", "Developer", new DateOnly(2020, 1, 1), null, null, []), Ct)).Value;

        var changed = await _fixture.SaveWork.HandleAsync(
            new SaveWorkExperience.Command(cv.Id, _user, job.Id, "Acme", "Lead", new DateOnly(2020, 1, 1), new DateOnly(2022, 1, 1), "  ", []), Ct);
        var backwards = await _fixture.SaveWork.HandleAsync(
            new SaveWorkExperience.Command(cv.Id, _user, job.Id, "Acme", "Lead", new DateOnly(2022, 1, 1), new DateOnly(2020, 1, 1), null, []), Ct);

        Assert.Equal("Lead", changed.Value.Role);
        Assert.Null(changed.Value.Description);
        Assert.Equal("cv_end_before_start", backwards.Error.Code);
    }

    [Theory]
    [InlineData("", "Developer", "cv_employer_required")]
    [InlineData("Acme", "", "cv_role_required")]
    public async Task WorkExperienceRequiresEmployerAndRole(string employer, string role, string expectedCode)
    {
        var cv = await NewCvAsync();

        var result = await _fixture.SaveWork.HandleAsync(
            new SaveWorkExperience.Command(cv.Id, _user, null, employer, role, new DateOnly(2020, 1, 1), null, null, []), Ct);

        Assert.Equal(expectedCode, result.Error.Code);
    }

    [Fact]
    public async Task SectionsCannotBeChangedThroughSomeoneElsesCvOrEntry()
    {
        var cv = await NewCvAsync();
        var other = await NewCvAsync(title: "Other");
        var course = (await _fixture.SaveCourse.HandleAsync(new SaveCourse.Command(cv.Id, _user, null, "Azure", null, null), Ct)).Value;
        var intruder = Guid.NewGuid();

        var foreign = await _fixture.SaveCourse.HandleAsync(new SaveCourse.Command(cv.Id, intruder, null, "Hack", null, null), Ct);
        var wrongCv = await _fixture.SaveCourse.HandleAsync(new SaveCourse.Command(other.Id, _user, course.Id, "Moved", null, null), Ct);
        var delete = await _fixture.DeleteItem.HandleAsync(new DeleteCvSectionItem.Command(cv.Id, intruder, CvSection.Course, course.Id), Ct);

        Assert.Equal("cv_not_found", foreign.Error.Code);
        Assert.Equal("cv_item_not_found", wrongCv.Error.Code);
        Assert.Equal("cv_not_found", delete.Error.Code);
    }

    [Theory]
    [InlineData(1949)]
    [InlineData(2027)]
    public async Task CourseYearMustBeReasonable(int year)
    {
        var cv = await NewCvAsync();

        var result = await _fixture.SaveCourse.HandleAsync(new SaveCourse.Command(cv.Id, _user, null, "Azure", null, year), Ct);

        Assert.Equal("cv_course_year_invalid", result.Error.Code);
    }

    [Fact]
    public async Task ALanguageAppearsOncePerCvIgnoringCase()
    {
        var cv = await NewCvAsync();
        await _fixture.SaveLanguage.HandleAsync(new SaveLanguageSkill.Command(cv.Id, _user, null, "English", LanguageLevel.Fluent), Ct);

        var duplicate = await _fixture.SaveLanguage.HandleAsync(new SaveLanguageSkill.Command(cv.Id, _user, null, "english", LanguageLevel.Good), Ct);

        Assert.Equal("cv_language_conflict", duplicate.Error.Code);
    }

    [Fact]
    public async Task DeleteRemovesTheEntryFromItsSectionOnly()
    {
        var cv = await NewCvAsync();
        var course = (await _fixture.SaveCourse.HandleAsync(new SaveCourse.Command(cv.Id, _user, null, "Azure", null, null), Ct)).Value;

        var wrongSection = await _fixture.DeleteItem.HandleAsync(new DeleteCvSectionItem.Command(cv.Id, _user, CvSection.Education, course.Id), Ct);
        var deleted = await _fixture.DeleteItem.HandleAsync(new DeleteCvSectionItem.Command(cv.Id, _user, CvSection.Course, course.Id), Ct);

        Assert.Equal("cv_item_not_found", wrongSection.Error.Code);
        Assert.True(deleted.IsSuccess);
    }

    [Fact]
    public async Task DeletingACvDeletesItsSections()
    {
        var cv = await NewCvAsync();
        await _fixture.SaveCourse.HandleAsync(new SaveCourse.Command(cv.Id, _user, null, "Azure", null, null), Ct);

        await _fixture.DeleteForUser.HandleAsync(new DeleteCvsForUser.Command(_user), Ct);

        await using var db = await _fixture.DbFactory.CreateDbContextAsync(Ct);
        Assert.Empty(db.Courses);
    }

    [Fact]
    public async Task DriverLicenceRejectsUnknownCategoriesAndCanBeCleared()
    {
        var cv = await NewCvAsync();

        var unknown = await _fixture.SetLicence.HandleAsync(new SetDriverLicence.Command(cv.Id, _user, ["Z"]), Ct);
        await _fixture.SetLicence.HandleAsync(new SetDriverLicence.Command(cv.Id, _user, ["B"]), Ct);
        var cleared = await _fixture.SetLicence.HandleAsync(new SetDriverLicence.Command(cv.Id, _user, []), Ct);

        Assert.Equal("cv_driver_licence_invalid", unknown.Error.Code);
        Assert.Empty(cleared.Value);
    }

    [Fact]
    public async Task PhotoMustBeASmallPngOrJpegAndCanBeRemoved()
    {
        var cv = await NewCvAsync();

        var text = await _fixture.SetPhoto.HandleAsync(new SetCvPhoto.Command(cv.Id, _user, "hello"u8.ToArray()), Ct);
        var big = await _fixture.SetPhoto.HandleAsync(new SetCvPhoto.Command(cv.Id, _user, new byte[SetCvPhoto.MaxPhotoBytes + 1]), Ct);
        var jpeg = await _fixture.SetPhoto.HandleAsync(new SetCvPhoto.Command(cv.Id, _user, [0xFF, 0xD8, 0xFF, 0xE0]), Ct);
        var removed = await _fixture.SetPhoto.HandleAsync(new SetCvPhoto.Command(cv.Id, _user, null), Ct);

        Assert.Equal("cv_photo_invalid", text.Error.Code);
        Assert.Equal("cv_photo_too_large", big.Error.Code);
        Assert.True(jpeg.IsSuccess);
        Assert.True(removed.IsSuccess);
        Assert.Null((await _fixture.GetDetail.HandleAsync(new GetCvDetail.Query(cv.Id, _user), Ct)).Value.Photo);
    }

    [Fact]
    public async Task TechnologiesAreTrimmedDeduplicatedAndKeptWithTheJob()
    {
        var cv = await NewCvAsync();

        var result = await _fixture.SaveWork.HandleAsync(
            new SaveWorkExperience.Command(cv.Id, _user, null, "Acme", "Developer", new DateOnly(2020, 1, 1), null, null, [" C# ", "c#", ".NET", "", "SQL, Docker"]), Ct);

        Assert.Equal(["C#", ".NET", "SQL", "Docker"], result.Value.Technologies);
        var detail = (await _fixture.GetDetail.HandleAsync(new GetCvDetail.Query(cv.Id, _user), Ct)).Value;
        Assert.Equal(["C#", ".NET", "SQL", "Docker"], detail.WorkExperiences[0].Technologies);
    }

    [Fact]
    public async Task TechnologiesAreLimitedInNumberAndLength()
    {
        var cv = await NewCvAsync();
        var many = Enumerable.Range(1, 21).Select(i => $"Tech{i}").ToList();

        var tooMany = await _fixture.SaveWork.HandleAsync(
            new SaveWorkExperience.Command(cv.Id, _user, null, "Acme", "Developer", new DateOnly(2020, 1, 1), null, null, many), Ct);
        var tooLong = await _fixture.SaveWork.HandleAsync(
            new SaveWorkExperience.Command(cv.Id, _user, null, "Acme", "Developer", new DateOnly(2020, 1, 1), null, null, [new string('x', 31)]), Ct);

        Assert.Equal("cv_technologies_too_many", tooMany.Error.Code);
        Assert.Equal("cv_technology_too_long", tooLong.Error.Code);
    }
}
