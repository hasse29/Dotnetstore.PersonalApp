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

        (detail.User?.FullName).ShouldBe("Anna Berg");
        detail.WorkExperiences.ShouldHaveSingleItem().Employer.ShouldBe("Acme");
        detail.WorkExperiences[0].EndDate.ShouldBeNull();
        detail.Educations.ShouldHaveSingleItem().Programme.ShouldBe("MSc");
        detail.Courses.ShouldHaveSingleItem().CompletedYear.ShouldBe(2022);
        detail.Languages.ShouldHaveSingleItem().Level.ShouldBe(LanguageLevel.Native);
        detail.DriverLicence.ShouldBe(["B", "BE"]);
        detail.Photo.ShouldBe(Png);
    }

    [Fact]
    public async Task DetailOfSomeoneElsesCvIsNotFound()
    {
        var cv = await NewCvAsync();

        var result = await _fixture.GetDetail.HandleAsync(new GetCvDetail.Query(cv.Id, Guid.NewGuid()), Ct);

        result.Error.Code.ShouldBe("cv_not_found");
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

        changed.Value.Role.ShouldBe("Lead");
        changed.Value.Description.ShouldBeNull();
        backwards.Error.Code.ShouldBe("cv_end_before_start");
    }

    [Theory]
    [InlineData("", "Developer", "cv_employer_required")]
    [InlineData("Acme", "", "cv_role_required")]
    public async Task WorkExperienceRequiresEmployerAndRole(string employer, string role, string expectedCode)
    {
        var cv = await NewCvAsync();

        var result = await _fixture.SaveWork.HandleAsync(
            new SaveWorkExperience.Command(cv.Id, _user, null, employer, role, new DateOnly(2020, 1, 1), null, null, []), Ct);

        result.Error.Code.ShouldBe(expectedCode);
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

        foreign.Error.Code.ShouldBe("cv_not_found");
        wrongCv.Error.Code.ShouldBe("cv_item_not_found");
        delete.Error.Code.ShouldBe("cv_not_found");
    }

    [Theory]
    [InlineData(1949)]
    [InlineData(2027)]
    public async Task CourseYearMustBeReasonable(int year)
    {
        var cv = await NewCvAsync();

        var result = await _fixture.SaveCourse.HandleAsync(new SaveCourse.Command(cv.Id, _user, null, "Azure", null, year), Ct);

        result.Error.Code.ShouldBe("cv_course_year_invalid");
    }

    [Fact]
    public async Task ALanguageAppearsOncePerCvIgnoringCase()
    {
        var cv = await NewCvAsync();
        await _fixture.SaveLanguage.HandleAsync(new SaveLanguageSkill.Command(cv.Id, _user, null, "English", LanguageLevel.Fluent), Ct);

        var duplicate = await _fixture.SaveLanguage.HandleAsync(new SaveLanguageSkill.Command(cv.Id, _user, null, "english", LanguageLevel.Good), Ct);

        duplicate.Error.Code.ShouldBe("cv_language_conflict");
    }

    [Fact]
    public async Task DeleteRemovesTheEntryFromItsSectionOnly()
    {
        var cv = await NewCvAsync();
        var course = (await _fixture.SaveCourse.HandleAsync(new SaveCourse.Command(cv.Id, _user, null, "Azure", null, null), Ct)).Value;

        var wrongSection = await _fixture.DeleteItem.HandleAsync(new DeleteCvSectionItem.Command(cv.Id, _user, CvSection.Education, course.Id), Ct);
        var deleted = await _fixture.DeleteItem.HandleAsync(new DeleteCvSectionItem.Command(cv.Id, _user, CvSection.Course, course.Id), Ct);

        wrongSection.Error.Code.ShouldBe("cv_item_not_found");
        deleted.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public async Task DeletingACvDeletesItsSections()
    {
        var cv = await NewCvAsync();
        await _fixture.SaveCourse.HandleAsync(new SaveCourse.Command(cv.Id, _user, null, "Azure", null, null), Ct);

        await _fixture.DeleteForUser.HandleAsync(new DeleteCvsForUser.Command(_user), Ct);

        await using var db = await _fixture.DbFactory.CreateDbContextAsync(Ct);
        db.Courses.ShouldBeEmpty();
    }

    [Fact]
    public async Task DriverLicenceRejectsUnknownCategoriesAndCanBeCleared()
    {
        var cv = await NewCvAsync();

        var unknown = await _fixture.SetLicence.HandleAsync(new SetDriverLicence.Command(cv.Id, _user, ["Z"]), Ct);
        await _fixture.SetLicence.HandleAsync(new SetDriverLicence.Command(cv.Id, _user, ["B"]), Ct);
        var cleared = await _fixture.SetLicence.HandleAsync(new SetDriverLicence.Command(cv.Id, _user, []), Ct);

        unknown.Error.Code.ShouldBe("cv_driver_licence_invalid");
        cleared.Value.ShouldBeEmpty();
    }

    [Fact]
    public async Task PhotoMustBeASmallPngOrJpegAndCanBeRemoved()
    {
        var cv = await NewCvAsync();

        var text = await _fixture.SetPhoto.HandleAsync(new SetCvPhoto.Command(cv.Id, _user, "hello"u8.ToArray()), Ct);
        var big = await _fixture.SetPhoto.HandleAsync(new SetCvPhoto.Command(cv.Id, _user, new byte[SetCvPhoto.MaxPhotoBytes + 1]), Ct);
        var jpeg = await _fixture.SetPhoto.HandleAsync(new SetCvPhoto.Command(cv.Id, _user, [0xFF, 0xD8, 0xFF, 0xE0]), Ct);
        var removed = await _fixture.SetPhoto.HandleAsync(new SetCvPhoto.Command(cv.Id, _user, null), Ct);

        text.Error.Code.ShouldBe("cv_photo_invalid");
        big.Error.Code.ShouldBe("cv_photo_too_large");
        jpeg.IsSuccess.ShouldBeTrue();
        removed.IsSuccess.ShouldBeTrue();
        (await _fixture.GetDetail.HandleAsync(new GetCvDetail.Query(cv.Id, _user), Ct)).Value.Photo.ShouldBeNull();
    }

    [Fact]
    public async Task TechnologiesAreTrimmedDeduplicatedAndKeptWithTheJob()
    {
        var cv = await NewCvAsync();

        var result = await _fixture.SaveWork.HandleAsync(
            new SaveWorkExperience.Command(cv.Id, _user, null, "Acme", "Developer", new DateOnly(2020, 1, 1), null, null, [" C# ", "c#", ".NET", "", "SQL, Docker"]), Ct);

        result.Value.Technologies.ShouldBe(["C#", ".NET", "SQL", "Docker"]);
        var detail = (await _fixture.GetDetail.HandleAsync(new GetCvDetail.Query(cv.Id, _user), Ct)).Value;
        detail.WorkExperiences[0].Technologies.ShouldBe(["C#", ".NET", "SQL", "Docker"]);
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

        tooMany.Error.Code.ShouldBe("cv_technologies_too_many");
        tooLong.Error.Code.ShouldBe("cv_technology_too_long");
    }
}
