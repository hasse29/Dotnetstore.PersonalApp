using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Dotnetstore.PersonalApp.Shared.Localization;
using Dotnetstore.PersonalApp.Shared.Mvvm;

namespace Dotnetstore.PersonalApp.Modules.Products.Features.StorageLocations;

internal sealed partial class StorageLocationEditorViewModel : ViewModelBase
{
    private readonly StorageLocationDto? _existing;
    private readonly CreateStorageLocation.Handler _create;
    private readonly UpdateStorageLocation.Handler _update;
    private readonly ILocalizationService _localization;

    public StorageLocationEditorViewModel(
        StorageLocationDto? existing,
        CreateStorageLocation.Handler create,
        UpdateStorageLocation.Handler update,
        ILocalizationService localization)
    {
        _existing = existing;
        _create = create;
        _update = update;
        _localization = localization;

        if (existing is not null)
        {
            Name = existing.Name;
            Description = existing.Description ?? string.Empty;
            IsActive = existing.IsActive;
        }
    }

    public event EventHandler<StorageLocationDto>? Saved;

    public event EventHandler? Cancelled;

    public string Title => _localization.Get(_existing is null ? "StorageLocationEditor_New" : "StorageLocationEditor_Edit");

    [ObservableProperty]
    public partial string Name { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Description { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool IsActive { get; set; } = true;

    [ObservableProperty]
    public partial string? ErrorMessage { get; set; }

    [RelayCommand]
    private async Task SaveAsync(CancellationToken cancellationToken)
    {
        ErrorMessage = null;

        var fields = new StorageLocationFields(Name, Description, IsActive);
        var result = _existing is null
            ? await _create.HandleAsync(new CreateStorageLocation.Command(fields), cancellationToken)
            : await _update.HandleAsync(new UpdateStorageLocation.Command(_existing.Id, fields), cancellationToken);

        if (result.IsSuccess)
        {
            Saved?.Invoke(this, result.Value);
        }
        else
        {
            ErrorMessage = _localization.Describe(result.Error);
        }
    }

    [RelayCommand]
    private void Cancel() => Cancelled?.Invoke(this, EventArgs.Empty);
}
