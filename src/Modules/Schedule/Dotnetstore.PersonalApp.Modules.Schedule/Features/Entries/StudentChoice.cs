using CommunityToolkit.Mvvm.ComponentModel;

namespace Dotnetstore.PersonalApp.Modules.Schedule.Features.Entries;

/// <summary>A student of the lesson's school with a check box saying whether they attend.</summary>
internal sealed partial class StudentChoice(Guid id, string name, bool isSelected) : ObservableObject
{
    public Guid Id { get; } = id;

    public string Name { get; } = name;

    [ObservableProperty]
    public partial bool IsSelected { get; set; } = isSelected;
}
