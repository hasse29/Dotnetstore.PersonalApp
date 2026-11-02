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

        Assert.True((await Set.HandleAsync(new SetClientPhoto.Command(id, Png), Ct)).IsSuccess);
        Assert.Equal(Png, (await Get.HandleAsync(new GetClientPhoto.Query(id), Ct)).Value);

        await Set.HandleAsync(new SetClientPhoto.Command(id, null), Ct);
        Assert.Null((await Get.HandleAsync(new GetClientPhoto.Query(id), Ct)).Value);
    }

    [Fact]
    public async Task InvalidAndOversizedFilesAreRejected()
    {
        var id = await CreateClientAsync();

        var notAnImage = await Set.HandleAsync(new SetClientPhoto.Command(id, [1, 2, 3, 4]), Ct);
        var tooLarge = await Set.HandleAsync(new SetClientPhoto.Command(id, [.. Png, .. new byte[PhotoRules.MaxBytes]]), Ct);

        Assert.Equal("photo_invalid", notAnImage.Error.Code);
        Assert.Equal("photo_too_large", tooLarge.Error.Code);
    }

    [Fact]
    public async Task AnUnknownClientIsReported()
    {
        var set = await Set.HandleAsync(new SetClientPhoto.Command(Guid.NewGuid(), Png), Ct);
        var get = await Get.HandleAsync(new GetClientPhoto.Query(Guid.NewGuid()), Ct);

        Assert.Equal("client_not_found", set.Error.Code);
        Assert.Equal("client_not_found", get.Error.Code);
    }

    [Fact]
    public async Task UpdatingAndListingKeepTheClientsPhoto()
    {
        var created = (await _fixture.Create.HandleAsync(ClientsFixture.ValidCommand(), Ct)).Value;
        await Set.HandleAsync(new SetClientPhoto.Command(created.Id, Png), Ct);

        var updated = await _fixture.Update.HandleAsync(
            new UpdateClient.Command(created.Id, "Renamed AB", created.OrganizationNumber, null, null, null, null, null), Ct);
        var listed = await _fixture.List.HandleAsync(new ListClients.Query(), Ct);

        Assert.True(updated.IsSuccess);
        Assert.Single(listed.Items);
        Assert.Equal(Png, (await Get.HandleAsync(new GetClientPhoto.Query(created.Id), Ct)).Value);
    }

    [Fact]
    public async Task ASquareThumbnailIsMadeAndListedWithTheClient()
    {
        var id = await CreateClientAsync();

        await Set.HandleAsync(new SetClientPhoto.Command(id, Png), Ct);
        var listed = (await _fixture.List.HandleAsync(new ListClients.Query(), Ct)).Items.Single();

        Assert.NotNull(listed.Thumbnail);
        using var thumbnail = SKBitmap.Decode(listed.Thumbnail);
        Assert.Equal((PhotoThumbnail.Size, PhotoThumbnail.Size), (thumbnail.Width, thumbnail.Height));
    }

    [Fact]
    public async Task AFileThatLooksLikeAnImageButCannotBeDecodedIsRejected()
    {
        var id = await CreateClientAsync();

        var result = await Set.HandleAsync(new SetClientPhoto.Command(id, [0x89, 0x50, 0x4E, 0x47, 0x00, 0x00]), Ct);

        Assert.Equal("photo_invalid", result.Error.Code);
    }
}
