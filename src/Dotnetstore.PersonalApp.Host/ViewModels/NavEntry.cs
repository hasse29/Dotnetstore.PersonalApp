using Avalonia;
using CommunityToolkit.Mvvm.ComponentModel;
using Dotnetstore.PersonalApp.Shared.Shell;

namespace Dotnetstore.PersonalApp.Host.ViewModels;

/// <summary>One row of the sidebar: a page, a group header that expands to its pages, or a page nested in a group.</summary>
internal sealed partial class NavEntry : ObservableObject
{
    public NavEntry(string title, string iconData, IModulePage? page, string? groupKey, bool isChild)
    {
        Title = title;
        IconData = iconData;
        Page = page;
        GroupKey = groupKey;
        IsChild = isChild;
    }

    public string Title { get; }

    public string IconData { get; }

    /// <summary>Null for a group header.</summary>
    public IModulePage? Page { get; }

    public string? GroupKey { get; }

    public bool IsChild { get; }

    public bool IsHeader => Page is null;

    public Thickness Indent => IsChild ? new Thickness(18, 0, 0, 0) : default;

    // Material "expand_more" / "chevron_right"
    public string ChevronData => IsExpanded
        ? "M16.59 8.59L12 13.17 7.41 8.59 6 10l6 6 6-6z"
        : "M10 6L8.59 7.41 13.17 12l-4.58 4.59L10 18l6-6z";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ChevronData))]
    public partial bool IsExpanded { get; set; }
}