using System.Collections.ObjectModel;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Dotnetstore.PersonalApp.Shared.Mvvm;

namespace Dotnetstore.PersonalApp.Modules.Cvs.Features.Cvs;

/// <summary>One row of a section on the detail page, already formatted for display.</summary>
internal sealed class CvSectionItem(string title, string? subtitle, string? meta, string? description, ICommand editCommand, IReadOnlyList<string>? tags = null)
{
    public string Title => title;

    public string? Subtitle => subtitle;

    public string? Meta => meta;

    public string? Description => description;

    public ICommand EditCommand => editCommand;

    /// <summary>Short labels shown as chips under the entry; empty for sections without them.</summary>
    public IReadOnlyList<string> Tags => tags ?? [];

    public bool HasTags => Tags.Count > 0;
}

/// <summary>A titled card on the detail page listing the entries of one section, with a button to add one.</summary>
internal sealed partial class CvSectionListViewModel(string title, string addText, string emptyText, Action add) : ViewModelBase
{
    public string Title => title;

    public string AddText => addText;

    public string EmptyText => emptyText;

    public ObservableCollection<CvSectionItem> Items { get; } = [];

    [ObservableProperty]
    public partial bool IsEmpty { get; set; } = true;

    public void SetItems(IEnumerable<CvSectionItem> items)
    {
        Items.Clear();
        foreach (var item in items)
        {
            Items.Add(item);
        }

        IsEmpty = Items.Count == 0;
    }

    [RelayCommand]
    private void Add() => add();
}

/// <summary>One technology in the CV-wide summary, with the number of jobs it was used in.</summary>
internal sealed record TechnologyChip(string Name, int Jobs)
{
    public string Text => Jobs > 1 ? $"{Name} ×{Jobs}" : Name;
}
