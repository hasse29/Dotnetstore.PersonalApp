using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Dotnetstore.PersonalApp.Modules.Inventory.Features.InventoryCountLines;
using Dotnetstore.PersonalApp.Modules.Inventory.Features.Warehouses;
using Dotnetstore.PersonalApp.Shared.Localization;

namespace Dotnetstore.PersonalApp.Modules.Inventory.Features.InventoryCounts;

/// <summary>One inventory count on one screen: the header, the counted lines, and the buttons that fill the list from stock and post the result.</summary>
internal sealed partial class InventoryCountScreenViewModel : DetailViewModel
{
    private readonly ListInventoryCounts.Handler _list;
    private readonly CreateInventoryCount.Handler _create;
    private readonly UpdateInventoryCount.Handler _update;
    private readonly ListWarehouseOptions.Handler _listWarehouseOptions;
    private readonly FillInventoryCount.Handler _fill;
    private readonly PostInventoryCount.Handler _post;
    private readonly TimeProvider _clock;
    private readonly ILocalizationService _localization;

    private IReadOnlyList<OptionDto> _warehouseOptions = [];

    public InventoryCountScreenViewModel(
        InventoryCountDto count,
        ListInventoryCounts.Handler list,
        CreateInventoryCount.Handler create,
        UpdateInventoryCount.Handler update,
        ListWarehouseOptions.Handler listWarehouseOptions,
        InventoryCountLinesViewModel lines,
        FillInventoryCount.Handler fill,
        PostInventoryCount.Handler post,
        TimeProvider clock,
        ILocalizationService localization)
    {
        _list = list;
        _create = create;
        _update = update;
        _listWarehouseOptions = listWarehouseOptions;
        _fill = fill;
        _post = post;
        _clock = clock;
        _localization = localization;

        Current = count;
        Lines = lines.ForParent(count.Id);
    }

    public InventoryCountLinesViewModel Lines { get; }

    /// <summary>The count as last saved; once posted it can no longer be filled or posted.</summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(FillCommand))]
    [NotifyCanExecuteChangedFor(nameof(PostCommand))]
    public partial InventoryCountDto Current { get; set; }

    [ObservableProperty]
    public partial InventoryCountEditorViewModel? Editor { get; set; }

    [ObservableProperty]
    public partial string? StatusMessage { get; set; }

    [ObservableProperty]
    public partial string? ErrorMessage { get; set; }

    [RelayCommand]
    private async Task LoadAsync(CancellationToken cancellationToken)
    {
        _warehouseOptions = await _listWarehouseOptions.HandleAsync(cancellationToken);

        OpenEditor(Current);
        Lines.LoadCommand.Execute(null);
    }

    [RelayCommand(CanExecute = nameof(IsOpen))]
    private async Task FillAsync(CancellationToken cancellationToken)
    {
        StatusMessage = null;
        ErrorMessage = null;

        var result = await _fill.HandleAsync(new FillInventoryCount.Command(Current.Id), cancellationToken);
        if (result.IsSuccess)
        {
            StatusMessage = _localization.Format("InventoryCountScreen_Filled", result.Value);
            Lines.LoadCommand.Execute(null);
        }
        else
        {
            ErrorMessage = _localization.Describe(result.Error);
        }
    }

    [RelayCommand(CanExecute = nameof(IsOpen))]
    private async Task PostAsync(CancellationToken cancellationToken)
    {
        StatusMessage = null;
        ErrorMessage = null;

        var result = await _post.HandleAsync(new PostInventoryCount.Command(Current.Id), cancellationToken);
        if (result.IsSuccess)
        {
            StatusMessage = _localization.Format("InventoryCountScreen_PostedResult", result.Value);
            await RefreshAsync(cancellationToken);
        }
        else
        {
            ErrorMessage = _localization.Describe(result.Error);
        }
    }

    private bool IsOpen() => !Current.Posted;

    private async Task RefreshAsync(CancellationToken cancellationToken)
    {
        var response = await _list.HandleAsync(new ListInventoryCounts.Query(Current.Number, ParentId: Current.WarehouseId), cancellationToken);
        if (response.Items.FirstOrDefault(c => c.Id == Current.Id) is { } fresh)
        {
            Current = fresh;
            OpenEditor(fresh);
        }
    }

    private void OpenEditor(InventoryCountDto count)
    {
        if (Editor is { } old)
        {
            old.Saved -= OnEditorSaved;
            old.Cancelled -= OnEditorCancelled;
        }

        var editor = new InventoryCountEditorViewModel(count, _create, _update, _warehouseOptions, _clock, _localization);
        editor.Saved += OnEditorSaved;
        editor.Cancelled += OnEditorCancelled;
        Editor = editor;
    }

    private void OnEditorSaved(object? sender, InventoryCountDto saved)
    {
        Current = saved;
        OpenEditor(saved);
    }

    private void OnEditorCancelled(object? sender, EventArgs e) => OpenEditor(Current);
}
