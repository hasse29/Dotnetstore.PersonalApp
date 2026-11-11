using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Dotnetstore.PersonalApp.Shared.Localization;
using Dotnetstore.PersonalApp.Shared.Mvvm;

namespace Dotnetstore.PersonalApp.Modules.Products.Features.InventoryCounts;

/// <summary>One line of the count: what the system expected and what was counted (editable while the count is open).</summary>
internal sealed partial class InventoryCountLineRow(InventoryCountLineDto line, string unitLabel, bool isEditable) : ObservableObject
{
    public InventoryCountLineDto Line { get; } = line;

    /// <summary>False once the count is posted.</summary>
    public bool IsEditable { get; } = isEditable;

    public string ExpectedText => string.Create(CultureInfo.CurrentCulture, $"{Line.ExpectedQuantity:0.##} {unitLabel}");

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DifferenceText))]
    public partial decimal? CountedQuantity { get; set; } = line.CountedQuantity;

    /// <summary>Counted minus expected; empty until the line has been counted.</summary>
    public string DifferenceText => CountedQuantity is { } counted
        ? string.Create(CultureInfo.CurrentCulture, $"{counted - Line.ExpectedQuantity:+0.##;-0.##;0}")
        : string.Empty;
}

/// <summary>The count itself: enter what was counted, save as you go, and post to adjust the stock.</summary>
internal sealed partial class InventoryCountScreenViewModel(
    InventoryCountDto count,
    GetInventoryCount.Handler get,
    SaveInventoryCountLines.Handler save,
    PostInventoryCount.Handler post,
    ILocalizationService localization) : ViewModelBase
{
    public InventoryCountDto Count { get; private set; } = count;

    public ObservableCollection<InventoryCountLineRow> Lines { get; } = [];

    public string Heading => Count.LocationName is { } name
        ? $"{Count.Name} · {name}"
        : $"{Count.Name} · {localization.Get("InventoryCounts_AllLocations")}";

    public bool IsOpen => !Count.IsPosted;

    public bool IsEmpty => Lines.Count == 0;

    [ObservableProperty]
    public partial string? StatusMessage { get; set; }

    [ObservableProperty]
    public partial bool IsConfirmingPost { get; set; }

    public event EventHandler? Closed;

    [RelayCommand]
    private async Task LoadAsync(CancellationToken cancellationToken)
    {
        var result = await get.HandleAsync(new GetInventoryCount.Query(Count.Id), cancellationToken);
        if (!result.IsSuccess)
        {
            StatusMessage = localization.Describe(result.Error);
            return;
        }

        Count = result.Value.Count;
        Lines.Clear();
        foreach (var line in result.Value.Lines)
        {
            Lines.Add(new InventoryCountLineRow(line, localization.Get($"ProductUnit_{line.Unit}"), !Count.IsPosted));
        }

        OnPropertyChanged(nameof(Heading));
        OnPropertyChanged(nameof(IsOpen));
        OnPropertyChanged(nameof(IsEmpty));
    }

    [RelayCommand]
    private async Task SaveAsync(CancellationToken cancellationToken)
    {
        if (await SaveLinesAsync(cancellationToken))
        {
            StatusMessage = localization.Get("InventoryCountScreen_Saved");
        }
    }

    [RelayCommand]
    private void RequestPost() => IsConfirmingPost = true;

    [RelayCommand]
    private void CancelPost() => IsConfirmingPost = false;

    [RelayCommand]
    private async Task ConfirmPostAsync(CancellationToken cancellationToken)
    {
        IsConfirmingPost = false;

        // Posting works on what is stored, so what is on screen is saved first.
        if (!await SaveLinesAsync(cancellationToken))
        {
            return;
        }

        var result = await post.HandleAsync(new PostInventoryCount.Command(Count.Id), cancellationToken);
        if (!result.IsSuccess)
        {
            StatusMessage = localization.Describe(result.Error);
            return;
        }

        await LoadAsync(cancellationToken);
        StatusMessage = localization.Format("InventoryCountScreen_Posted", result.Value);
    }

    [RelayCommand]
    private void Back() => Closed?.Invoke(this, EventArgs.Empty);

    private async Task<bool> SaveLinesAsync(CancellationToken cancellationToken)
    {
        var entries = Lines.Select(l => new SaveInventoryCountLines.Entry(l.Line.Id, l.CountedQuantity)).ToList();
        var result = await save.HandleAsync(new SaveInventoryCountLines.Command(Count.Id, entries), cancellationToken);
        if (result.IsSuccess)
        {
            return true;
        }

        StatusMessage = localization.Describe(result.Error);
        return false;
    }
}
