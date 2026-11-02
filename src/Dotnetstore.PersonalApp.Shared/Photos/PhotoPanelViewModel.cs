using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Dotnetstore.PersonalApp.Shared.Files;
using Dotnetstore.PersonalApp.Shared.Localization;
using Dotnetstore.PersonalApp.Shared.Mvvm;
using Dotnetstore.PersonalApp.Shared.Results;

namespace Dotnetstore.PersonalApp.Shared.Photos;

/// <summary>
/// The round photo (or initials) with Change and Remove buttons that the Users and Clients detail pages share. The owning module
/// supplies how the photo is read and stored, so this knows nothing about either.
/// </summary>
public sealed partial class PhotoPanelViewModel(
    string initials,
    Func<CancellationToken, Task<byte[]?>> load,
    Func<byte[]?, CancellationToken, Task<Result<Guid>>> save,
    IFileOpenService fileOpen,
    ILocalizationService localization) : ViewModelBase
{
    private bool _loaded;

    public string Initials { get; } = initials;

    public bool HasNoPhoto => Photo.IsEmpty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasNoPhoto))]
    [NotifyCanExecuteChangedFor(nameof(RemovePhotoCommand))]
    public partial ReadOnlyMemory<byte> Photo { get; set; }

    [ObservableProperty]
    public partial string? StatusMessage { get; set; }

    /// <summary>Reads the stored photo once; the view calls this when it is shown.</summary>
    [RelayCommand]
    private async Task LoadAsync(CancellationToken cancellationToken)
    {
        if (_loaded)
        {
            return;
        }

        Photo = await load(cancellationToken) ?? default;
        _loaded = true;
    }

    [RelayCommand]
    private async Task ChoosePhotoAsync(CancellationToken cancellationToken)
    {
        var file = await fileOpen.PickImageAsync(localization.Get("Photo_ChooseTitle"), PhotoRules.MaxBytes, cancellationToken);
        if (file is null)
        {
            StatusMessage = localization.Get("Photo_NotChanged");
            return;
        }

        await SetPhotoAsync(file.Content.ToArray(), cancellationToken);
    }

    [RelayCommand(CanExecute = nameof(HasPhoto))]
    private async Task RemovePhotoAsync(CancellationToken cancellationToken) => await SetPhotoAsync(null, cancellationToken);

    private bool HasPhoto() => !Photo.IsEmpty;

    private async Task SetPhotoAsync(byte[]? photo, CancellationToken cancellationToken)
    {
        var result = await save(photo, cancellationToken);
        if (result.IsSuccess)
        {
            _loaded = true;
            Photo = photo ?? default;
            StatusMessage = null;
        }
        else
        {
            StatusMessage = localization.Describe(result.Error);
        }
    }
}
