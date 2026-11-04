using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Dotnetstore.PersonalApp.Modules.Production.Features;
using Dotnetstore.PersonalApp.Shared.Localization;
using Dotnetstore.PersonalApp.Shared.Mvvm;

namespace Dotnetstore.PersonalApp.Modules.Production.Features.UnitMeasures;

internal sealed partial class UnitMeasureEditorViewModel : ViewModelBase
{
    private readonly UnitMeasureDto? _existing;
    private readonly CreateUnitMeasure.Handler _create;
    private readonly UpdateUnitMeasure.Handler _update;
    private readonly ILocalizationService _localization;

    public UnitMeasureEditorViewModel(
        UnitMeasureDto? existing,
        CreateUnitMeasure.Handler create,
        UpdateUnitMeasure.Handler update,
        TimeProvider clock,
        ILocalizationService localization)
    {
        _existing = existing;
        _create = create;
        _update = update;
        _localization = localization;



        if (existing is not null)
        {
            Code = existing.Code;
            Name = existing.Name;
        }
        else
        {
            // Every field starts empty.
        }
    }

    public event EventHandler<UnitMeasureDto>? Saved;

    public event EventHandler? Cancelled;

    public string Title => _localization.Get(_existing is null ? "UnitMeasureEditor_New" : "UnitMeasureEditor_Edit");

    [ObservableProperty]
    public partial string Code { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Name { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string? ErrorMessage { get; set; }

    [RelayCommand]
    private async Task SaveAsync(CancellationToken cancellationToken)
    {
        ErrorMessage = null;

        var result = _existing is not { } existing
            ? await _create.HandleAsync(new CreateUnitMeasure.Command(Code, Name), cancellationToken)
            : await _update.HandleAsync(new UpdateUnitMeasure.Command(existing.Id, Code, Name), cancellationToken);

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