using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Dotnetstore.PersonalApp.Shared.Localization;
using Dotnetstore.PersonalApp.Shared.Mvvm;

namespace Dotnetstore.PersonalApp.Modules.Cvs.Features.Cvs;

internal sealed partial class CvEditorViewModel : ViewModelBase
{
    private readonly CvDto? _existing;
    private readonly Guid _userId;
    private readonly CreateCv.Handler _create;
    private readonly UpdateCv.Handler _update;
    private readonly ILocalizationService _localization;

    public CvEditorViewModel(
        CvDto? existing,
        Guid userId,
        CreateCv.Handler create,
        UpdateCv.Handler update,
        ILocalizationService localization)
    {
        _existing = existing;
        _userId = userId;
        _create = create;
        _update = update;
        _localization = localization;

        if (existing is not null)
        {
            Title = existing.Title;
            TargetJob = existing.TargetJob ?? string.Empty;
            Summary = existing.Summary ?? string.Empty;
            LinkedIn = existing.LinkedIn ?? string.Empty;
        }
    }

    public event EventHandler<CvDto>? Saved;

    public event EventHandler? Cancelled;

    public string EditorTitle => _localization.Get(_existing is null ? "CvEditor_New" : "CvEditor_Edit");

    [ObservableProperty]
    public partial string Title { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string TargetJob { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Summary { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string LinkedIn { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string? ErrorMessage { get; set; }

    [RelayCommand]
    private async Task SaveAsync(CancellationToken cancellationToken)
    {
        ErrorMessage = null;

        var result = _existing is null
            ? await _create.HandleAsync(new CreateCv.Command(_userId, Title, TargetJob, Summary, LinkedIn), cancellationToken)
            : await _update.HandleAsync(new UpdateCv.Command(_existing.Id, _userId, Title, TargetJob, Summary, LinkedIn), cancellationToken);

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
