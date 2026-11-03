using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Dotnetstore.PersonalApp.Modules.Grades.Features.Scales;

/// <summary>One editable step in the scale editor. <see cref="Id"/> is null for a step that has not been saved yet.</summary>
internal sealed partial class GradeItemRow(Guid? id, Action<GradeItemRow> remove) : ObservableObject
{
    public Guid? Id { get; } = id;

    [ObservableProperty]
    public partial string Label { get; set; } = string.Empty;

    /// <summary>A decimal because that is what the numeric box binds to; it is always a whole number when saved.</summary>
    [ObservableProperty]
    public partial decimal? Rank { get; set; } = 0;

    [ObservableProperty]
    public partial string Description { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool IsPassing { get; set; } = true;

    [RelayCommand]
    private void Remove() => remove(this);

    public GradeScaleItemInput ToInput() => new(Id, Label, (int)(Rank ?? 0), Description, IsPassing);
}
