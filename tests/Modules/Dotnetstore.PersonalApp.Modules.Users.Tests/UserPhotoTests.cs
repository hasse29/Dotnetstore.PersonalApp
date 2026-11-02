using Dotnetstore.PersonalApp.Modules.Users.Features.Users;
using Dotnetstore.PersonalApp.Shared.Contracts.Events;
using Dotnetstore.PersonalApp.Shared.Files;
using Dotnetstore.PersonalApp.Shared.Localization;
using Dotnetstore.PersonalApp.Shared.Photos;
using SkiaSharp;
using Dotnetstore.PersonalApp.Shared.Results;

namespace Dotnetstore.PersonalApp.Modules.Users.Tests;

public sealed class UserPhotoTests : IDisposable
{
    private static readonly byte[] Png = TestImage.Create(SKEncodedImageFormat.Png);
    private static readonly byte[] Jpeg = TestImage.Create(SKEncodedImageFormat.Jpeg);

    private readonly UsersFixture _fixture = new();

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public void Dispose() => _fixture.Dispose();

    private SetUserPhoto.Handler Set => new(_fixture.DbFactory, _fixture.Events);

    private GetUserPhoto.Handler Get => new(_fixture.DbFactory);

    private async Task<Guid> CreateUserAsync() =>
        (await _fixture.Create.HandleAsync(UsersFixture.ValidCommand(), Ct)).Value.Id;

    [Fact]
    public async Task APhotoIsStoredAndReadBack()
    {
        var id = await CreateUserAsync();

        var set = await Set.HandleAsync(new SetUserPhoto.Command(id, Png), Ct);
        var read = await Get.HandleAsync(new GetUserPhoto.Query(id), Ct);

        Assert.True(set.IsSuccess);
        Assert.Equal(Png, read.Value);
    }

    [Fact]
    public async Task ANullPhotoRemovesIt()
    {
        var id = await CreateUserAsync();
        await Set.HandleAsync(new SetUserPhoto.Command(id, Jpeg), Ct);

        await Set.HandleAsync(new SetUserPhoto.Command(id, null), Ct);

        Assert.Null((await Get.HandleAsync(new GetUserPhoto.Query(id), Ct)).Value);
    }

    [Fact]
    public async Task OnlyPngAndJpegUpTo2MbAreAccepted()
    {
        var id = await CreateUserAsync();

        var notAnImage = await Set.HandleAsync(new SetUserPhoto.Command(id, [1, 2, 3, 4]), Ct);
        var tooLarge = await Set.HandleAsync(new SetUserPhoto.Command(id, [.. Png, .. new byte[PhotoRules.MaxBytes]]), Ct);

        Assert.Equal("photo_invalid", notAnImage.Error.Code);
        Assert.Equal("photo_too_large", tooLarge.Error.Code);
        Assert.Null((await Get.HandleAsync(new GetUserPhoto.Query(id), Ct)).Value);
    }

    [Fact]
    public async Task AnUnknownUserIsReported()
    {
        var set = await Set.HandleAsync(new SetUserPhoto.Command(Guid.NewGuid(), Png), Ct);
        var get = await Get.HandleAsync(new GetUserPhoto.Query(Guid.NewGuid()), Ct);

        Assert.Equal("user_not_found", set.Error.Code);
        Assert.Equal("user_not_found", get.Error.Code);
    }

    [Fact]
    public async Task UpdatingTheUserKeepsThePhoto()
    {
        var created = (await _fixture.Create.HandleAsync(UsersFixture.ValidCommand(), Ct)).Value;
        await Set.HandleAsync(new SetUserPhoto.Command(created.Id, Png), Ct);

        var updated = await _fixture.Update.HandleAsync(
            new UpdateUser.Command(created.Id, "Grace", created.LastName, created.Username, null, created.DateOfBirth), Ct);

        Assert.True(updated.IsSuccess);
        Assert.Equal(Png, (await Get.HandleAsync(new GetUserPhoto.Query(created.Id), Ct)).Value);
    }

    [Fact]
    public async Task ThePanelLoadsChoosesAndRemovesThePhoto()
    {
        var id = await CreateUserAsync();
        await Set.HandleAsync(new SetUserPhoto.Command(id, Png), Ct);
        var picker = new FakeFileOpen();
        var panel = new UserPhotoPanelFactory(Get, Set, new Localizer(), picker).Create(id, "AL")!;

        await panel.LoadCommand.ExecuteAsync(null);
        Assert.Equal(Png, panel.Photo.ToArray());
        Assert.False(panel.HasNoPhoto);

        picker.Next = new PickedFile("me.jpg", Jpeg);
        await panel.ChoosePhotoCommand.ExecuteAsync(null);
        Assert.Equal(Jpeg, panel.Photo.ToArray());
        Assert.Equal(Jpeg, (await Get.HandleAsync(new GetUserPhoto.Query(id), Ct)).Value);

        await panel.RemovePhotoCommand.ExecuteAsync(null);
        Assert.True(panel.HasNoPhoto);
        Assert.Null((await Get.HandleAsync(new GetUserPhoto.Query(id), Ct)).Value);
    }

    [Fact]
    public async Task ThePanelShowsAnErrorForAnInvalidFileAndKeepsTheOldPhoto()
    {
        var id = await CreateUserAsync();
        await Set.HandleAsync(new SetUserPhoto.Command(id, Png), Ct);
        var picker = new FakeFileOpen { Next = new PickedFile("notes.txt", new byte[] { 1, 2, 3, 4 }) };
        var panel = new UserPhotoPanelFactory(Get, Set, new Localizer(), picker).Create(id, "AL")!;
        await panel.LoadCommand.ExecuteAsync(null);

        await panel.ChoosePhotoCommand.ExecuteAsync(null);

        Assert.Equal(Png, panel.Photo.ToArray());
        Assert.False(string.IsNullOrEmpty(panel.StatusMessage));
    }

    [Fact]
    public async Task ACancelledPickLeavesThePhotoAndSaysSo()
    {
        var id = await CreateUserAsync();
        var panel = new UserPhotoPanelFactory(Get, Set, new Localizer(), new FakeFileOpen()).Create(id, "AL")!;

        await panel.ChoosePhotoCommand.ExecuteAsync(null);

        Assert.True(panel.HasNoPhoto);
        Assert.False(string.IsNullOrEmpty(panel.StatusMessage));
    }

    [Fact]
    public async Task WithoutAFileDialogThereIsNoPanel()
    {
        var id = await CreateUserAsync();

        Assert.Null(new UserPhotoPanelFactory(Get, Set, new Localizer()).Create(id, "AL"));
    }

    private sealed class FakeFileOpen : IFileOpenService
    {
        public PickedFile? Next { get; set; }

        public Task<PickedFile?> PickImageAsync(string title, long maxBytes, CancellationToken cancellationToken) => Task.FromResult(Next);
    }

    [Fact]
    public async Task ASquareThumbnailIsMadeListedAndClearedWithThePhoto()
    {
        var id = await CreateUserAsync();

        await Set.HandleAsync(new SetUserPhoto.Command(id, Png), Ct);
        var listed = (await _fixture.List.HandleAsync(new ListUsers.Query(), Ct)).Items.Single();

        Assert.NotNull(listed.Thumbnail);
        using var thumbnail = SKBitmap.Decode(listed.Thumbnail);
        Assert.Equal((PhotoThumbnail.Size, PhotoThumbnail.Size), (thumbnail.Width, thumbnail.Height));

        await Set.HandleAsync(new SetUserPhoto.Command(id, null), Ct);
        Assert.Null((await _fixture.List.HandleAsync(new ListUsers.Query(), Ct)).Items.Single().Thumbnail);
    }

    [Fact]
    public async Task AFileThatLooksLikeAnImageButCannotBeDecodedIsRejected()
    {
        var id = await CreateUserAsync();

        var result = await Set.HandleAsync(new SetUserPhoto.Command(id, [0x89, 0x50, 0x4E, 0x47, 0x00, 0x00]), Ct);

        Assert.Equal("photo_invalid", result.Error.Code);
    }

    [Fact]
    public async Task ChangingThePhotoTellsTheOtherModulesAndTheLookupReturnsTheThumbnail()
    {
        var id = await CreateUserAsync();

        await Set.HandleAsync(new SetUserPhoto.Command(id, Png), Ct);

        Assert.Contains(new UserPhotoChanged(id), _fixture.Events.Published);
        var lookup = new UserLookup(_fixture.DbFactory);
        Assert.NotNull(await lookup.GetThumbnailAsync(id, Ct));
        Assert.Null(await lookup.GetThumbnailAsync(Guid.NewGuid(), Ct));
    }
}
