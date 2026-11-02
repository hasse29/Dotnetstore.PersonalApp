using Dotnetstore.PersonalApp.Modules.Students.Features.Students;
using Dotnetstore.PersonalApp.Shared.Photos;
using SkiaSharp;

namespace Dotnetstore.PersonalApp.Modules.Students.Tests;

public sealed class StudentPhotoTests : IDisposable
{
    private static readonly byte[] Png = TestImage.Create(SKEncodedImageFormat.Png);

    private readonly StudentsFixture _fixture = new();

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public void Dispose() => _fixture.Dispose();

    private SetStudentPhoto.Handler Set => new(_fixture.DbFactory);

    private GetStudentPhoto.Handler Get => new(_fixture.DbFactory);

    private async Task<StudentDto> CreateStudentAsync() =>
        (await _fixture.Create.HandleAsync(new CreateStudent.Command(Guid.NewGuid(), "Anna", "Svensson", null, null), Ct)).Value;

    [Fact]
    public async Task APhotoIsStoredReadBackListedAsAThumbnailAndRemoved()
    {
        var student = await CreateStudentAsync();

        (await Set.HandleAsync(new SetStudentPhoto.Command(student.Id, Png), Ct)).IsSuccess.ShouldBeTrue();

        (await Get.HandleAsync(new GetStudentPhoto.Query(student.Id), Ct)).Value.ShouldBe(Png);
        var listed = (await _fixture.List.HandleAsync(new ListStudents.Query(student.ClientId), Ct)).ShouldHaveSingleItem();
        listed.Thumbnail.ShouldNotBeNull();
        using var thumbnail = SKBitmap.Decode(listed.Thumbnail);
        thumbnail.Width.ShouldBe(PhotoThumbnail.Size);

        await Set.HandleAsync(new SetStudentPhoto.Command(student.Id, null), Ct);
        (await Get.HandleAsync(new GetStudentPhoto.Query(student.Id), Ct)).Value.ShouldBeNull();
        (await _fixture.List.HandleAsync(new ListStudents.Query(student.ClientId), Ct)).ShouldHaveSingleItem().Thumbnail.ShouldBeNull();
    }

    [Fact]
    public async Task InvalidFilesAndUnknownStudentsAreRejected()
    {
        var student = await CreateStudentAsync();

        var notAnImage = await Set.HandleAsync(new SetStudentPhoto.Command(student.Id, [1, 2, 3, 4]), Ct);
        var unknown = await Set.HandleAsync(new SetStudentPhoto.Command(Guid.NewGuid(), Png), Ct);

        notAnImage.Error.Code.ShouldBe("photo_invalid");
        unknown.Error.Code.ShouldBe("student_not_found");
    }

    [Fact]
    public async Task UpdatingTheStudentKeepsThePhoto()
    {
        var student = await CreateStudentAsync();
        await Set.HandleAsync(new SetStudentPhoto.Command(student.Id, Png), Ct);

        await _fixture.Update.HandleAsync(new UpdateStudent.Command(student.Id, "Anna", "Berg", "8A", null), Ct);

        (await Get.HandleAsync(new GetStudentPhoto.Query(student.Id), Ct)).Value.ShouldBe(Png);
    }
}
