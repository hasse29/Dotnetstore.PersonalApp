using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Dotnetstore.PersonalApp.Modules.Products.Features.StorageLocations;
using Dotnetstore.PersonalApp.Shared.Localization;
using Dotnetstore.PersonalApp.Shared.Mvvm;

namespace Dotnetstore.PersonalApp.Modules.Products.Features.InventoryCounts;

/// <summary>A count as the list shows it: the DTO plus text in the current UI language.</summary>
internal sealed record InventoryCountRow(InventoryCountDto Count, string DateText, string LocationText, string ProgressText, string StatusText);

internal sealed partial class InventoryCountsViewModel(
    ListInventoryCounts.Handler list,
    CreateInventoryCount.Handler create,
    DeleteInventoryCount.Handler delete,
    GetInventoryCount.Handler get,
    SaveInventoryCountLines.Handler save,
    PostInventoryCount.Handler post,
    ListStorageLocations.Handler locations,
    TimeProvider clock,
    ILocalizationService localization) : ViewModelBase
{
    public ObservableCollection<InventoryCountRow> Counts { get; } = [];

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(OpenCommand))]
    [NotifyCanExecuteChangedFor(nameof(RequestDeleteCommand))]
    [NotifyCanExecuteChangedFor(nameof(ConfirmDeleteCommand))]
    public partial InventoryCountRow? SelectedCount { get; set; }

    [ObservableProperty]
    public partial InventoryCountEditorViewModel? Editor { get; set; }

    [ObservableProperty]
    public partial InventoryCountScreenViewModel? Screen { get; set; }

    [ObservableProperty]
    public partial string? StatusMessage { get; set; }

    [ObservableProperty]
    public partial bool IsConfirmingDelete { get; set; }

    partial void OnSelectedCountChanged(InventoryCountRow? value) => IsConfirmingDelete = false;

    [RelayCommand]
    private async Task LoadAsync(CancellationToken cancellationToken)
    {
        var response = await list.HandleAsync(new ListInventoryCounts.Query(), cancellationToken);

        Counts.Clear();
        foreach (var count in response.Items)
        {
            Counts.Add(ToRow(count));
        }

        StatusMessage = localization.Format("InventoryCounts_Count", response.TotalCount);
    }

    [RelayCommand]
    private async Task NewAsync(CancellationToken cancellationToken)
    {
        var all = (await locations.HandleAsync(new ListStorageLocations.Query(ActiveOnly: true), cancellationToken)).Items;
        var options = new List<CountLocationOption> { new(null, localization.Get("InventoryCounts_AllLocations")) };
        options.AddRange(all.Select(l => new CountLocationOption(l.Id, l.Name)));

        DetachEditor();
        var editor = new InventoryCountEditorViewModel(options, create, localization, clock.GetLocalNow());
        Editor = editor;
        editor.Saved += OnEditorSaved;
        editor.Cancelled += OnEditorCancelled;
    }

    [RelayCommand(CanExecute = nameof(HasSelection))]
    private void Open()
    {
        if (SelectedCount is { } row)
        {
            OpenScreen(row.Count);
        }
    }

    [RelayCommand(CanExecute = nameof(HasSelection))]
    private void RequestDelete() => IsConfirmingDelete = true;

    [RelayCommand]
    private void CancelDelete() => IsConfirmingDelete = false;

    [RelayCommand(CanExecute = nameof(HasSelection))]
    private async Task ConfirmDeleteAsync(CancellationToken cancellationToken)
    {
        if (SelectedCount is not { } row)
        {
            return;
        }

        var result = await delete.HandleAsync(new DeleteInventoryCount.Command(row.Count.Id), cancellationToken);

        IsConfirmingDelete = false;
        SelectedCount = null;
        await LoadAsync(cancellationToken);
        if (!result.IsSuccess)
        {
            StatusMessage = localization.Describe(result.Error);
        }
    }

    private bool HasSelection() => SelectedCount is not null;

    private void OpenScreen(InventoryCountDto count)
    {
        var screen = new InventoryCountScreenViewModel(count, get, save, post, localization);
        screen.Closed += OnScreenClosed;
        Screen = screen;
        screen.LoadCommand.Execute(null);
    }

    private void OnScreenClosed(object? sender, EventArgs e)
    {
        if (Screen is { } screen)
        {
            screen.Closed -= OnScreenClosed;
        }

        Screen = null;
        LoadCommand.Execute(null);
    }

    private void DetachEditor()
    {
        if (Editor is { } editor)
        {
            editor.Saved -= OnEditorSaved;
            editor.Cancelled -= OnEditorCancelled;
        }
    }

    // A new count opens straight away so counting can begin.
    private void OnEditorSaved(object? sender, InventoryCountDto saved)
    {
        DetachEditor();
        Editor = null;
        OpenScreen(saved);
    }

    private void OnEditorCancelled(object? sender, EventArgs e)
    {
        DetachEditor();
        Editor = null;
    }

    private InventoryCountRow ToRow(InventoryCountDto count) => new(
        count,
        string.Create(CultureInfo.CurrentCulture, $"{count.CreatedAt.ToLocalTime():d}"),
        count.LocationName ?? localization.Get("InventoryCounts_AllLocations"),
        string.Create(CultureInfo.CurrentCulture, $"{count.CountedLineCount} / {count.LineCount}"),
        localization.Get(count.IsPosted ? "InventoryCounts_Posted" : "InventoryCounts_Open"));
}
