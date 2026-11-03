using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Dotnetstore.PersonalApp.Shared.Localization;
using Dotnetstore.PersonalApp.Shared.Mvvm;

namespace Dotnetstore.PersonalApp.Modules.Grades.Features.Scales;

internal sealed partial class GradeScaleEditorViewModel : ViewModelBase
{
    private readonly GradeScaleDto? _existing;
    private readonly CreateGradeScale.Handler _create;
    private readonly UpdateGradeScale.Handler _update;
    private readonly ILocalizationService _localization;

    public GradeScaleEditorViewModel(
        GradeScaleDto? existing,
        CreateGradeScale.Handler create,
        UpdateGradeScale.Handler update,
        ILocalizationService localization)
    {
        _existing = existing;
        _create = create;
        _update = update;
        _localization = localization;

        if (existing is not null)
        {
            Name = existing.Name;
            Country = existing.Country ?? string.Empty;
            Description = existing.Description ?? string.Empty;
            foreach (var item in existing.Items)
            {
                Items.Add(new GradeItemRow(item.Id, RemoveItem)
                {
                    Label = item.Label,
                    Rank = item.Rank,
                    Description = item.Description ?? string.Empty,
                    IsPassing = item.IsPassing,
                });
            }
        }
    }

    public event EventHandler<GradeScaleDto>? Saved;

    public event EventHandler? Cancelled;

    public string Title => _localization.Get(_existing is null ? "GradeScaleEditor_New" : "GradeScaleEditor_Edit");

    public ObservableCollection<GradeItemRow> Items { get; } = [];

    [ObservableProperty]
    public partial string Name { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Country { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Description { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string? ErrorMessage { get; set; }

    private void RemoveItem(GradeItemRow row) => Items.Remove(row);

    /// <summary>A new step starts one below the lowest existing one, so typing the steps best to worst needs no extra work.</summary>
    [RelayCommand]
    private void AddItem()
    {
        var lowest = Items.Count == 0 ? (decimal?)null : Items.Min(i => i.Rank ?? 0);
        Items.Add(new GradeItemRow(null, RemoveItem) { Rank = lowest is { } rank ? Math.Max(0, rank - 1) : 1 });
    }

    [RelayCommand]
    private async Task SaveAsync(CancellationToken cancellationToken)
    {
        ErrorMessage = null;
        IReadOnlyList<GradeScaleItemInput> items = [.. Items.Select(i => i.ToInput())];

        var result = _existing is null
            ? await _create.HandleAsync(new CreateGradeScale.Command(Name, Country, Description, items), cancellationToken)
            : await _update.HandleAsync(new UpdateGradeScale.Command(_existing.Id, Name, Country, Description, items), cancellationToken);

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
