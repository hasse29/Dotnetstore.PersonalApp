using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Dotnetstore.PersonalApp.Shared.Localization;
using Dotnetstore.PersonalApp.Shared.Mvvm;
using Dotnetstore.PersonalApp.Shared.Results;

namespace Dotnetstore.PersonalApp.Modules.Grades.Features.Tests;

/// <summary>The name, date and maximum score of one test.</summary>
internal sealed partial class TestEditorViewModel : ViewModelBase
{
    private readonly Guid _userId;
    private readonly TestDto? _existing;
    private readonly CreateTest.Handler _create;
    private readonly UpdateTest.Handler _update;
    private readonly ILocalizationService _localization;

    public TestEditorViewModel(
        Guid userId,
        TestDto? existing,
        CreateTest.Handler create,
        UpdateTest.Handler update,
        ILocalizationService localization,
        TimeProvider clock)
    {
        _userId = userId;
        _existing = existing;
        _create = create;
        _update = update;
        _localization = localization;
        Date = clock.GetLocalNow().Date;

        if (existing is not null)
        {
            Name = existing.Name;
            Date = existing.Date.ToDateTime(TimeOnly.MinValue);
            MaxScoreText = ScoreText.Format(existing.MaxScore);
        }
    }

    public event EventHandler<TestDto>? Saved;

    public event EventHandler? Cancelled;

    public string Title => _localization.Get(_existing is null ? "TestEditor_New" : "TestEditor_Edit");

    [ObservableProperty]
    public partial string Name { get; set; } = string.Empty;

    /// <summary>A <see cref="DateTime"/> because that is what the date picker binds to.</summary>
    [ObservableProperty]
    public partial DateTime? Date { get; set; }

    [ObservableProperty]
    public partial string MaxScoreText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string? ErrorMessage { get; set; }

    [RelayCommand]
    private async Task SaveAsync(CancellationToken cancellationToken)
    {
        ErrorMessage = null;

        var date = DateOnly.FromDateTime(Date ?? DateTime.MinValue);
        var maxScore = ScoreText.Parse(MaxScoreText);

        var result = _existing is null
            ? await _create.HandleAsync(new CreateTest.Command(_userId, Name, date, maxScore), cancellationToken)
            : await _update.HandleAsync(new UpdateTest.Command(_existing.Id, _userId, Name, date, maxScore), cancellationToken);

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
