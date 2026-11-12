using Dotnetstore.PersonalApp.Shared.Files;
using Dotnetstore.PersonalApp.Shared.Localization;
using Dotnetstore.PersonalApp.Shared.Photos;
using Dotnetstore.PersonalApp.Shared.Results;

namespace Dotnetstore.PersonalApp.Integration.Tests;

public sealed class PhotoPanelViewModelTests
{
    private readonly Localizer _localizer = new();
    private readonly FakeFileOpen _fileOpen = new();
    private byte[]? _stored;
    private int _loads;
    private Result<Guid> _saveResult = Guid.NewGuid();

    public PhotoPanelViewModelTests() => _localizer.SetCulture("en");

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private PhotoPanelViewModel Panel(byte[]? stored = null)
    {
        _stored = stored;
        return new PhotoPanelViewModel(
            "AA",
            _ =>
            {
                _loads++;
                return Task.FromResult(_stored);
            },
            (photo, _) =>
            {
                if (_saveResult.IsSuccess)
                {
                    _stored = photo;
                }

                return Task.FromResult(_saveResult);
            },
            _fileOpen,
            _localizer);
    }

    [Fact]
    public void ThePanelStartsEmptyWithItsInitials()
    {
        var panel = Panel();

        panel.Initials.ShouldBe("AA");
        panel.HasNoPhoto.ShouldBeTrue();
        panel.RemovePhotoCommand.CanExecute(null).ShouldBeFalse();
    }

    [Fact]
    public async Task LoadingShowsTheStoredPhotoAndOnlyReadsOnce()
    {
        var panel = Panel([1, 2, 3]);

        await panel.LoadCommand.ExecuteAsync(null);
        await panel.LoadCommand.ExecuteAsync(null);

        panel.Photo.ToArray().ShouldBe(new byte[] { 1, 2, 3 });
        panel.HasNoPhoto.ShouldBeFalse();
        panel.RemovePhotoCommand.CanExecute(null).ShouldBeTrue();
        _loads.ShouldBe(1);
    }

    [Fact]
    public async Task LoadingWithNothingStoredLeavesThePanelEmpty()
    {
        var panel = Panel();

        await panel.LoadCommand.ExecuteAsync(null);

        panel.HasNoPhoto.ShouldBeTrue();
    }

    [Fact]
    public async Task ChoosingAPhotoSavesItAndShowsIt()
    {
        var panel = Panel();
        _fileOpen.Next = new PickedFile("me.png", new byte[] { 9, 8, 7 });

        await panel.ChoosePhotoCommand.ExecuteAsync(null);

        _stored.ShouldBe(new byte[] { 9, 8, 7 });
        panel.Photo.ToArray().ShouldBe(new byte[] { 9, 8, 7 });
        panel.StatusMessage.ShouldBeNull();
        panel.HasNoPhoto.ShouldBeFalse();
        _fileOpen.LastMaxBytes.ShouldBe(PhotoRules.MaxBytes);
        _fileOpen.LastTitle.ShouldBe(_localizer.Get("Photo_ChooseTitle"));
    }

    [Fact]
    public async Task ACancelledPickLeavesThePhotoAndExplainsWhy()
    {
        var panel = Panel([1]);
        await panel.LoadCommand.ExecuteAsync(null);
        _fileOpen.Next = null;

        await panel.ChoosePhotoCommand.ExecuteAsync(null);

        panel.Photo.ToArray().ShouldBe(new byte[] { 1 });
        panel.StatusMessage.ShouldBe(_localizer.Get("Photo_NotChanged"));
    }

    [Fact]
    public async Task ARejectedPhotoShowsTheErrorAndKeepsTheOldOne()
    {
        var panel = Panel([1]);
        await panel.LoadCommand.ExecuteAsync(null);
        _saveResult = Error.Validation("photo_invalid", "The photo is not valid.");
        _fileOpen.Next = new PickedFile("bad.txt", new byte[] { 5 });

        await panel.ChoosePhotoCommand.ExecuteAsync(null);

        panel.Photo.ToArray().ShouldBe(new byte[] { 1 });
        panel.StatusMessage.ShouldBe(_localizer.Describe(Error.Validation("photo_invalid", "The photo is not valid.")));
    }

    [Fact]
    public async Task RemovingThePhotoClearsItAndAFailedRemoveKeepsIt()
    {
        var panel = Panel([1, 2]);
        await panel.LoadCommand.ExecuteAsync(null);

        _saveResult = Error.Conflict("photo_remove_failed", "Could not remove.");
        await panel.RemovePhotoCommand.ExecuteAsync(null);
        panel.HasNoPhoto.ShouldBeFalse();
        panel.StatusMessage.ShouldNotBeNull();

        _saveResult = Guid.NewGuid();
        await panel.RemovePhotoCommand.ExecuteAsync(null);
        panel.HasNoPhoto.ShouldBeTrue();
        panel.StatusMessage.ShouldBeNull();
        _stored.ShouldBeNull();
        panel.RemovePhotoCommand.CanExecute(null).ShouldBeFalse();
    }

    [Fact]
    public async Task APhotoSavedBeforeLoadingIsNotOverwrittenByALaterLoad()
    {
        var panel = Panel([1]);
        _fileOpen.Next = new PickedFile("me.png", new byte[] { 4, 4 });
        await panel.ChoosePhotoCommand.ExecuteAsync(null);

        await panel.LoadCommand.ExecuteAsync(null);

        panel.Photo.ToArray().ShouldBe(new byte[] { 4, 4 });
        _loads.ShouldBe(0);
    }

    private sealed class FakeFileOpen : IFileOpenService
    {
        public PickedFile? Next { get; set; }

        public string? LastTitle { get; private set; }

        public long LastMaxBytes { get; private set; }

        public Task<PickedFile?> PickImageAsync(string title, long maxBytes, CancellationToken cancellationToken)
        {
            LastTitle = title;
            LastMaxBytes = maxBytes;
            return Task.FromResult(Next);
        }
    }
}
