using DocumentFormat.OpenXml.Packaging;
using Dotnetstore.PersonalApp.Modules.Cvs.Domain;
using Dotnetstore.PersonalApp.Modules.Cvs.Features.Cvs;
using Dotnetstore.PersonalApp.Shared.Users;

namespace Dotnetstore.PersonalApp.Modules.Cvs.Tests;

public sealed class CvExportTests : IDisposable
{
    private static readonly byte[] Png = Convert.FromBase64String(
        "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mP8z8BQDwAEhQGAhKmMIQAAAABJRU5ErkJggg==");

    private readonly CvsFixture _fixture = new();
    private readonly Guid _user = Guid.NewGuid();

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public void Dispose() => _fixture.Dispose();

    private async Task<Guid> FullCvAsync()
    {
        _fixture.Users.User = new UserReference(_user, "Anna", "Berg", "anna", new DateOnly(1990, 5, 17));
        var cv = (await _fixture.Create.HandleAsync(new CreateCv.Command(_user, "Backend", "Senior developer", "Line one\nLine two"), Ct)).Value;
        await _fixture.SaveWork.HandleAsync(new SaveWorkExperience.Command(
            cv.Id, _user, null, "Acme", "Developer", new DateOnly(2020, 1, 1), null, "Built things", ["C#", "SQL"]), Ct);
        await _fixture.SaveEducation.HandleAsync(new SaveEducation.Command(cv.Id, _user, null, "KTH", "MSc", new DateOnly(2010, 9, 1), new DateOnly(2015, 6, 1), null), Ct);
        await _fixture.SaveCourse.HandleAsync(new SaveCourse.Command(cv.Id, _user, null, "Azure", "Microsoft", 2022), Ct);
        await _fixture.SaveLanguage.HandleAsync(new SaveLanguageSkill.Command(cv.Id, _user, null, "Swedish", LanguageLevel.Native), Ct);
        await _fixture.SetLicence.HandleAsync(new SetDriverLicence.Command(cv.Id, _user, ["B"]), Ct);
        await _fixture.SetPhoto.HandleAsync(new SetCvPhoto.Command(cv.Id, _user, Png), Ct);
        return cv.Id;
    }

    [Fact]
    public async Task ExportProducesAWordDocumentWithEverySection()
    {
        var id = await FullCvAsync();

        var result = await _fixture.Export.HandleAsync(new ExportCvToWord.Query(id, _user), Ct);

        Assert.True(result.IsSuccess);
        Assert.Equal("CV - Anna Berg - Backend.docx", result.Value.FileName);
        using var stream = new MemoryStream(result.Value.Content);
        using var document = WordprocessingDocument.Open(stream, false);
        var text = document.MainDocumentPart!.Document!.Body!.InnerText;
        foreach (var expected in new[] { "Anna Berg", "Senior developer", "Line one", "Developer, Acme", "2020-01 – present", "C#", "MSc, KTH", "Azure — Microsoft — 2022", "Swedish — Native", "Work experience", "Languages" })
        {
            Assert.Contains(expected, text, StringComparison.Ordinal);
        }

        Assert.Single(document.MainDocumentPart.ImageParts);
    }

    [Fact]
    public async Task ExportOfAnEmptyCvWithoutPhotoStillWorks()
    {
        var cv = (await _fixture.Create.HandleAsync(CvsFixture.ValidCv(_user), Ct)).Value;

        var result = await _fixture.Export.HandleAsync(new ExportCvToWord.Query(cv.Id, _user), Ct);

        Assert.True(result.IsSuccess);
        using var stream = new MemoryStream(result.Value.Content);
        using var document = WordprocessingDocument.Open(stream, false);
        Assert.Empty(document.MainDocumentPart!.ImageParts);
    }

    [Fact]
    public async Task ExportOfSomeoneElsesCvIsNotFound()
    {
        var id = await FullCvAsync();

        var result = await _fixture.Export.HandleAsync(new ExportCvToWord.Query(id, Guid.NewGuid()), Ct);

        Assert.Equal("cv_not_found", result.Error.Code);
    }

    [Fact]
    public async Task ExportCommandHandsTheFileToTheSaveDialog()
    {
        var id = await FullCvAsync();
        var cv = (await _fixture.Get.HandleAsync(new GetCv.Query(id, _user), Ct)).Value;
        var viewModel = new CvDetailViewModel(cv, _user, _fixture.DetailServices);

        await viewModel.ExportCommand.ExecuteAsync(null);

        Assert.Equal("CV - Anna Berg - Backend.docx", _fixture.FileSave.SavedName);
        Assert.NotEmpty(_fixture.FileSave.SavedContent!);
    }

    [Theory]
    [InlineData("iVBORw0KGgoAAAANSUhEUgAAAAMAAAAFCAYAAACNbyblAAAAC0lEQVR4nGNgAAIAAAUAAXpeqz8AAAAASUVORK5CYII=", 3, 5)]
    public void ImageSizeReadsPngDimensions(string base64, int width, int height) =>
        Assert.Equal((width, height), ImageSize.TryRead(Convert.FromBase64String(base64)));

    [Fact]
    public void ImageSizeReadsJpegDimensions()
    {
        // SOI, then an APP0 segment of length 4, then SOF0 (height 7, width 9).
        byte[] jpeg = [0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x04, 0x00, 0x00, 0xFF, 0xC0, 0x00, 0x0B, 0x08, 0x00, 0x07, 0x00, 0x09, 0x01, 0x01, 0x11, 0x00];

        Assert.Equal((9, 7), ImageSize.TryRead(jpeg));
    }
}
