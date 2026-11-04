using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Dotnetstore.PersonalApp.Modules.Production.Features;
using Dotnetstore.PersonalApp.Shared.Localization;
using Dotnetstore.PersonalApp.Shared.Mvvm;

namespace Dotnetstore.PersonalApp.Modules.Production.Features.ScrapReasons;

internal sealed partial class ScrapReasonEditorViewModel : ViewModelBase
{
    private readonly ScrapReasonDto? _existing;
    private readonly CreateScrapReason.Handler _create;
    private readonly UpdateScrapReason.Handler _update;
    private readonly ILocalizationService _localization;

    public ScrapReasonEditorViewModel(
        ScrapReasonDto? existing,
        CreateScrapReason.Handler create,
        UpdateScrapReason.Handler update,
        TimeProvider clock,
        ILocalizationService localization)
    {
        _existing = existing;
        _create = create;
        _update = update;
        _localization = localization;



        if (existing is not null)
        {
            Name = existing.Name;
        }
        else
        {
            // Every field starts empty.
        }
    }

    public event EventHandler<ScrapReasonDto>? Saved;

    public event EventHandler? Cancelled;

    public string Title => _localization.Get(_existing is null ? "ScrapReasonEditor_New" : "ScrapReasonEditor_Edit");

    [ObservableProperty]
    public partial string Name { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string? ErrorMessage { get; set; }

    [RelayCommand]
    private async Task SaveAsync(CancellationToken cancellationToken)
    {
        ErrorMessage = null;

        var result = _existing is not { } existing
            ? await _create.HandleAsync(new CreateScrapReason.Command(Name), cancellationToken)
            : await _update.HandleAsync(new UpdateScrapReason.Command(existing.Id, Name), cancellationToken);

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