using Dotnetstore.PersonalApp.Host.Events;
using Dotnetstore.PersonalApp.Host.ViewModels;
using Dotnetstore.PersonalApp.Shared.Auth;
using Dotnetstore.PersonalApp.Shared.Contracts.Events;
using Dotnetstore.PersonalApp.Shared.Shell;
using Dotnetstore.PersonalApp.Shared.Users;

namespace Dotnetstore.PersonalApp.Integration.Tests;

public sealed class ShellPhotoTests
{
    private static readonly AuthenticatedUser User = new(Guid.NewGuid(), "ada", "Ada Lovelace");

    private sealed class FakePhotos(byte[]? thumbnail) : IUserPhotoLookup
    {
        public Task<byte[]?> GetThumbnailAsync(Guid userId, CancellationToken cancellationToken) =>
            Task.FromResult(userId == User.Id ? thumbnail : null);
    }

    [Fact]
    public async Task TheShellLoadsTheSignedInUsersThumbnail()
    {
        using var shell = new ShellViewModel(Array.Empty<IModulePage>(), User, () => { }, null, new FakePhotos([1, 2, 3]));

        await shell.LoadUserPhotoCommand.ExecuteAsync(null);

        shell.UserPhoto.ShouldBe(new byte[] { 1, 2, 3 });
        shell.Initials.ShouldBe("AL");
    }

    [Fact]
    public async Task WithoutAPhotoTheShellKeepsShowingInitials()
    {
        using var shell = new ShellViewModel(Array.Empty<IModulePage>(), User, () => { }, null, new FakePhotos(null));

        await shell.LoadUserPhotoCommand.ExecuteAsync(null);

        shell.UserPhoto.ShouldBeNull();
    }

    [Fact]
    public async Task TheNotifierRaisesTheEventForThePublishedUser()
    {
        var notifier = new UserPhotoNotifier();
        Guid? changed = null;
        notifier.Changed += (_, id) => changed = id;

        await notifier.ConsumeAsync(new UserPhotoChanged(User.Id), TestContext.Current.CancellationToken);

        changed.ShouldBe(User.Id);
    }
}
