using Dotnetstore.PersonalApp.Modules.Clients.Features.ContactPersons;
using Dotnetstore.PersonalApp.Shared.Photos;
using SkiaSharp;

namespace Dotnetstore.PersonalApp.Modules.Clients.Tests;

public sealed class ContactPersonPhotoTests : IDisposable
{
    private static readonly byte[] Png = TestImage.Create(SKEncodedImageFormat.Png);

    private readonly ClientsFixture _fixture = new();

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public void Dispose() => _fixture.Dispose();

    private SetContactPersonPhoto.Handler Set => new(_fixture.DbFactory);

    private GetContactPersonPhoto.Handler Get => new(_fixture.DbFactory);

    private async Task<ContactPersonDto> CreatePersonAsync()
    {
        var clientId = (await _fixture.Create.HandleAsync(ClientsFixture.ValidCommand(), Ct)).Value.Id;
        return (await _fixture.CreateContactPerson.HandleAsync(new CreateContactPerson.Command(clientId, "Anna", "Svensson", null, null), Ct)).Value;
    }

    [Fact]
    public async Task APhotoIsStoredReadBackListedAsAThumbnailAndRemoved()
    {
        var person = await CreatePersonAsync();

        (await Set.HandleAsync(new SetContactPersonPhoto.Command(person.Id, Png), Ct)).IsSuccess.ShouldBeTrue();

        (await Get.HandleAsync(new GetContactPersonPhoto.Query(person.Id), Ct)).Value.ShouldBe(Png);
        var listed = (await _fixture.ListContactPersons.HandleAsync(new ListContactPersons.Query(person.ClientId), Ct)).Single();
        listed.Thumbnail.ShouldNotBeNull();
        using var thumbnail = SKBitmap.Decode(listed.Thumbnail);
        thumbnail.Width.ShouldBe(PhotoThumbnail.Size);

        await Set.HandleAsync(new SetContactPersonPhoto.Command(person.Id, null), Ct);
        (await Get.HandleAsync(new GetContactPersonPhoto.Query(person.Id), Ct)).Value.ShouldBeNull();
        (await _fixture.ListContactPersons.HandleAsync(new ListContactPersons.Query(person.ClientId), Ct)).Single().Thumbnail.ShouldBeNull();
    }

    [Fact]
    public async Task InvalidFilesAndUnknownPeopleAreRejected()
    {
        var person = await CreatePersonAsync();

        var notAnImage = await Set.HandleAsync(new SetContactPersonPhoto.Command(person.Id, [1, 2, 3, 4]), Ct);
        var unknown = await Set.HandleAsync(new SetContactPersonPhoto.Command(Guid.NewGuid(), Png), Ct);

        notAnImage.Error.Code.ShouldBe("photo_invalid");
        unknown.Error.Code.ShouldBe("contact_person_not_found");
    }

    [Fact]
    public async Task UpdatingThePersonKeepsThePhoto()
    {
        var person = await CreatePersonAsync();
        await Set.HandleAsync(new SetContactPersonPhoto.Command(person.Id, Png), Ct);

        await _fixture.UpdateContactPerson.HandleAsync(new UpdateContactPerson.Command(person.Id, "Anna", "Berg", "CFO", null), Ct);

        (await Get.HandleAsync(new GetContactPersonPhoto.Query(person.Id), Ct)).Value.ShouldBe(Png);
    }
}
