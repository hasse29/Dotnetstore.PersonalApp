using Dotnetstore.PersonalApp.Modules.Clients.Features.Clients;
using Dotnetstore.PersonalApp.Shared.Photos;
using SkiaSharp;

namespace Dotnetstore.PersonalApp.Modules.Clients.Tests;

public sealed class ClientPhotoTests : IDisposable
{
    private static readonly byte[] Png = TestImage.Create(SKEncodedImageFormat.Png);

    private readonly ClientsFixture _fixture = new();

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public void Dispose() => _fixture.Dispose();

    private SetClientPhoto.Handler Set => new(_fixture.DbFactory);

    private GetClientPhoto.Handler Get => new(_fixture.DbFactory);

    private async Task<Guid> CreateClientAsync() =>
        (await _fixture.Create.HandleAsync(ClientsFixture.ValidCommand(), Ct)).Value.Id;

    [Fact]
    public async Task APhotoIsStoredReadBackAndRemoved()
    {
        var id = await CreateClientAsync();

        (await Set.HandleAsync(new SetClientPhoto.Command(id, Png), Ct)).IsSuccess.ShouldBeTrue();
        (await Get.HandleAsync(new GetClientPhoto.Query(id), Ct)).Value.ShouldBe(Png);

        await Set.HandleAsync(new SetClientPhoto.Command(id, null), Ct);
        (await Get.HandleAsync(new GetClientPhoto.Query(id), Ct)).Value.ShouldBeNull();
    }

    [Fact]
    public async Task InvalidAndOversizedFilesAreRejected()
    {
        var id = await CreateClientAsync();

        var notAnImage = await Set.HandleAsync(new SetClientPhoto.Command(id, [1, 2, 3, 4]), Ct);
        var tooLarge = await Set.HandleAsync(new SetClientPhoto.Command(id, [.. Png, .. new byte[PhotoRules.MaxBytes]]), Ct);

        notAnImage.Error.Code.ShouldBe("photo_invalid");
        tooLarge.Error.Code.ShouldBe("photo_too_large");
    }

    [Fact]
    public async Task AnUnknownClientIsReported()
    {
        var set = await Set.HandleAsync(new SetClientPhoto.Command(Guid.NewGuid(), Png), Ct);
        var get = await Get.HandleAsync(new GetClientPhoto.Query(Guid.NewGuid()), Ct);

        set.Error.Code.ShouldBe("client_not_found");
        get.Error.Code.ShouldBe("client_not_found");
    }

    [Fact]
    public async Task UpdatingAndListingKeepTheClientsPhoto()
    {
        var created = (await _fixture.Create.HandleAsync(ClientsFixture.ValidCommand(), Ct)).Value;
        await Set.HandleAsync(new SetClientPhoto.Command(created.Id, Png), Ct);

        var updated = await _fixture.Update.HandleAsync(
            new UpdateClient.Command(created.Id, "Renamed AB", created.OrganizationNumber, null, null, null, null, null), Ct);
        var listed = await _fixture.List.HandleAsync(new ListClients.Query(), Ct);

        updated.IsSuccess.ShouldBeTrue();
        listed.Items.ShouldHaveSingleItem();
        (await Get.HandleAsync(new GetClientPhoto.Query(created.Id), Ct)).Value.ShouldBe(Png);
    }

    [Fact]
    public async Task ASquareThumbnailIsMadeAndListedWithTheClient()
    {
        var id = await CreateClientAsync();

        await Set.HandleAsync(new SetClientPhoto.Command(id, Png), Ct);
        var listed = (await _fixture.List.HandleAsync(new ListClients.Query(), Ct)).Items.Single();

        listed.Thumbnail.ShouldNotBeNull();
        using var thumbnail = SKBitmap.Decode(listed.Thumbnail);
        (thumbnail.Width, thumbnail.Height).ShouldBe((PhotoThumbnail.Size, PhotoThumbnail.Size));
    }

    [Fact]
    public async Task AFileThatLooksLikeAnImageButCannotBeDecodedIsRejected()
    {
        var id = await CreateClientAsync();

        var result = await Set.HandleAsync(new SetClientPhoto.Command(id, [0x89, 0x50, 0x4E, 0x47, 0x00, 0x00]), Ct);

        result.Error.Code.ShouldBe("photo_invalid");
    }
}
