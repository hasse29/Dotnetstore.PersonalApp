using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Dotnetstore.PersonalApp.Shared.Auth;
using Dotnetstore.PersonalApp.Shared.Mvvm;
using Dotnetstore.PersonalApp.Shared.Shell;

namespace Dotnetstore.PersonalApp.Host.ViewModels;

/// <summary>The signed-in experience: module navigation on the left, the selected module page on the right.</summary>
internal sealed partial class ShellViewModel : ViewModelBase, IDisposable
{
    private readonly Action _signOut;
    private readonly ModuleSettings? _moduleSettings;
    private readonly IReadOnlyList<IModulePage> _allPages;
    private List<NavEntry> _allEntries;
    private readonly HashSet<string> _expandedGroups = [];
    private bool _rebuilding;

    public ShellViewModel(IEnumerable<IModulePage> pages, AuthenticatedUser user, Action signOut, ModuleSettings? moduleSettings = null)
    {
        _moduleSettings = moduleSettings;
        _signOut = signOut;
        User = user;
        _allPages = [.. pages];
        Pages = VisiblePages();
        _allEntries = BuildEntries(Pages);
        Entries = [];
        SelectedPage = Pages.Count > 0 ? Pages[0] : null;

        if (_moduleSettings is not null)
        {
            _moduleSettings.Changed += OnModulesChanged;
        }
    }

    public void Dispose()
    {
        if (_moduleSettings is not null)
        {
            _moduleSettings.Changed -= OnModulesChanged;
        }
    }

    private List<IModulePage> VisiblePages() =>
        [.. _allPages.Where(page => page.ModuleKey is not { } key || _moduleSettings?.IsEnabled(key) != false)];

    private void OnModulesChanged(object? sender, EventArgs e)
    {
        Pages = VisiblePages();
        _allEntries = BuildEntries(Pages);

        if (SelectedPage is null || !Pages.Contains(SelectedPage))
        {
            SelectedPage = Pages.Count > 0 ? Pages[0] : null;
        }
        else
        {
            Rebuild();
        }
    }

    public AuthenticatedUser User { get; }

    public string Initials => string.Concat(
        User.FullName.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Take(2)
            .Select(part => char.ToUpperInvariant(part[0])));

    public IReadOnlyList<IModulePage> Pages { get; private set; }

    /// <summary>The visible sidebar rows: top-level pages and group headers, plus the pages of expanded groups.</summary>
    public ObservableCollection<NavEntry> Entries { get; }

    [ObservableProperty]
    public partial IModulePage? SelectedPage { get; set; }

    [ObservableProperty]
    public partial NavEntry? SelectedEntry { get; set; }

    [ObservableProperty]
    public partial ViewModelBase? CurrentPage { get; set; }

    partial void OnSelectedPageChanged(IModulePage? value)
    {
        CurrentPage = value?.CreateViewModel();

        if (value?.Group is { } group)
        {
            _expandedGroups.Add(group.Key);
        }

        Rebuild();
    }

    partial void OnSelectedEntryChanged(NavEntry? value)
    {
        if (_rebuilding || value is null)
        {
            return;
        }

        if (value.Page is { } page)
        {
            SelectedPage = page;
            return;
        }

        // A header only expands or collapses; keep the current page highlighted.
        if (!_expandedGroups.Remove(value.GroupKey!))
        {
            _expandedGroups.Add(value.GroupKey!);
        }

        Rebuild();
    }

    private void Rebuild()
    {
        _rebuilding = true;
        try
        {
            Entries.Clear();
            foreach (var entry in _allEntries)
            {
                if (entry.IsChild && !_expandedGroups.Contains(entry.GroupKey!))
                {
                    continue;
                }

                if (entry.IsHeader)
                {
                    entry.IsExpanded = _expandedGroups.Contains(entry.GroupKey!);
                }

                Entries.Add(entry);
            }

            SelectedEntry = Entries.FirstOrDefault(entry => entry.Page == SelectedPage);
        }
        finally
        {
            _rebuilding = false;
        }
    }

    private static List<NavEntry> BuildEntries(IReadOnlyList<IModulePage> pages)
    {
        // Top-level entries (ungrouped pages and groups) keep registration order, then sort by the group's Order.
        var topLevel = new List<(int Order, NavEntry Header, List<NavEntry> Children)>();
        var groups = new Dictionary<string, List<NavEntry>>();

        foreach (var page in pages)
        {
            if (page.Group is not { } group)
            {
                topLevel.Add((0, new NavEntry(page.Title, page.IconData, page, null, false), []));
                continue;
            }

            if (!groups.TryGetValue(group.Key, out var children))
            {
                children = [];
                groups[group.Key] = children;
                topLevel.Add((group.Order, new NavEntry(group.Title, group.IconData, null, group.Key, false), children));
            }

            children.Add(new NavEntry(page.Title, page.IconData, page, group.Key, true));
        }

        return [.. topLevel
            .Select((item, index) => (item, index))
            .OrderBy(x => x.item.Order)
            .ThenBy(x => x.index)
            .SelectMany(x => new[] { x.item.Header }.Concat(x.item.Children))];
    }

    [RelayCommand]
    private void SignOut() => _signOut();
}