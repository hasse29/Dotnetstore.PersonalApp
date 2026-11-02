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

        set.IsSuccess.ShouldBeTrue();
        read.Value.ShouldBe(Png);
    }

    [Fact]
    public async Task ANullPhotoRemovesIt()
    {
        var id = await CreateUserAsync();
        await Set.HandleAsync(new SetUserPhoto.Command(id, Jpeg), Ct);

        await Set.HandleAsync(new SetUserPhoto.Command(id, null), Ct);

        (await Get.HandleAsync(new GetUserPhoto.Query(id), Ct)).Value.ShouldBeNull();
    }

    [Fact]
    public async Task OnlyPngAndJpegUpTo2MbAreAccepted()
    {
        var id = await CreateUserAsync();

        var notAnImage = await Set.HandleAsync(new SetUserPhoto.Command(id, [1, 2, 3, 4]), Ct);
        var tooLarge = await Set.HandleAsync(new SetUserPhoto.Command(id, [.. Png, .. new byte[PhotoRules.MaxBytes]]), Ct);

        notAnImage.Error.Code.ShouldBe("photo_invalid");
        tooLarge.Error.Code.ShouldBe("photo_too_large");
        (await Get.HandleAsync(new GetUserPhoto.Query(id), Ct)).Value.ShouldBeNull();
    }

    [Fact]
    public async Task AnUnknownUserIsReported()
    {
        var set = await Set.HandleAsync(new SetUserPhoto.Command(Guid.NewGuid(), Png), Ct);
        var get = await Get.HandleAsync(new GetUserPhoto.Query(Guid.NewGuid()), Ct);

        set.Error.Code.ShouldBe("user_not_found");
        get.Error.Code.ShouldBe("user_not_found");
    }

    [Fact]
    public async Task UpdatingTheUserKeepsThePhoto()
    {
        var created = (await _fixture.Create.HandleAsync(UsersFixture.ValidCommand(), Ct)).Value;
        await Set.HandleAsync(new SetUserPhoto.Command(created.Id, Png), Ct);

        var updated = await _fixture.Update.HandleAsync(
            new UpdateUser.Command(created.Id, "Grace", created.LastName, created.Username, null, created.DateOfBirth), Ct);

        updated.IsSuccess.ShouldBeTrue();
        (await Get.HandleAsync(new GetUserPhoto.Query(created.Id), Ct)).Value.ShouldBe(Png);
    }

    [Fact]
    public async Task ThePanelLoadsChoosesAndRemovesThePhoto()
    {
        var id = await CreateUserAsync();
        await Set.HandleAsync(new SetUserPhoto.Command(id, Png), Ct);
        var picker = new FakeFileOpen();
        var panel = new UserPhotoPanelFactory(Get, Set, new Localizer(), picker).Create(id, "AL")!;

        await panel.LoadCommand.ExecuteAsync(null);
        panel.Photo.ToArray().ShouldBe(Png);
        panel.HasNoPhoto.ShouldBeFalse();

        picker.Next = new PickedFile("me.jpg", Jpeg);
        await panel.ChoosePhotoCommand.ExecuteAsync(null);
        panel.Photo.ToArray().ShouldBe(Jpeg);
        (await Get.HandleAsync(new GetUserPhoto.Query(id), Ct)).Value.ShouldBe(Jpeg);

        await panel.RemovePhotoCommand.ExecuteAsync(null);
        panel.HasNoPhoto.ShouldBeTrue();
        (await Get.HandleAsync(new GetUserPhoto.Query(id), Ct)).Value.ShouldBeNull();
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

        panel.Photo.ToArray().ShouldBe(Png);
        string.IsNullOrEmpty(panel.StatusMessage).ShouldBeFalse();
    }

    [Fact]
    public async Task ACancelledPickLeavesThePhotoAndSaysSo()
    {
        var id = await CreateUserAsync();
        var panel = new UserPhotoPanelFactory(Get, Set, new Localizer(), new FakeFileOpen()).Create(id, "AL")!;

        await panel.ChoosePhotoCommand.ExecuteAsync(null);

        panel.HasNoPhoto.ShouldBeTrue();
        string.IsNullOrEmpty(panel.StatusMessage).ShouldBeFalse();
    }

    [Fact]
    public async Task WithoutAFileDialogThereIsNoPanel()
    {
        var id = await CreateUserAsync();

        (new UserPhotoPanelFactory(Get, Set, new Localizer()).Create(id, "AL")).ShouldBeNull();
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

        listed.Thumbnail.ShouldNotBeNull();
        using var thumbnail = SKBitmap.Decode(listed.Thumbnail);
        (thumbnail.Width, thumbnail.Height).ShouldBe((PhotoThumbnail.Size, PhotoThumbnail.Size));

        await Set.HandleAsync(new SetUserPhoto.Command(id, null), Ct);
        (await _fixture.List.HandleAsync(new ListUsers.Query(), Ct)).Items.Single().Thumbnail.ShouldBeNull();
    }

    [Fact]
    public async Task AFileThatLooksLikeAnImageButCannotBeDecodedIsRejected()
    {
        var id = await CreateUserAsync();

        var result = await Set.HandleAsync(new SetUserPhoto.Command(id, [0x89, 0x50, 0x4E, 0x47, 0x00, 0x00]), Ct);

        result.Error.Code.ShouldBe("photo_invalid");
    }

    [Fact]
    public async Task ChangingThePhotoTellsTheOtherModulesAndTheLookupReturnsTheThumbnail()
    {
        var id = await CreateUserAsync();

        await Set.HandleAsync(new SetUserPhoto.Command(id, Png), Ct);

        _fixture.Events.Published.ShouldContain(new UserPhotoChanged(id));
        var lookup = new UserLookup(_fixture.DbFactory);
        (await lookup.GetThumbnailAsync(id, Ct)).ShouldNotBeNull();
        (await lookup.GetThumbnailAsync(Guid.NewGuid(), Ct)).ShouldBeNull();
    }
}
