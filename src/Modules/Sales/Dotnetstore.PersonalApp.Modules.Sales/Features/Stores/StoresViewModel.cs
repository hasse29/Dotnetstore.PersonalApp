using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Dotnetstore.PersonalApp.Modules.Sales.Features.SalesPeople;
using Dotnetstore.PersonalApp.Modules.Sales.Features;
using Dotnetstore.PersonalApp.Shared.Localization;
using Dotnetstore.PersonalApp.Shared.Mvvm;

namespace Dotnetstore.PersonalApp.Modules.Sales.Features.Stores;

internal sealed partial class StoresViewModel(
    ListStores.Handler list,
    CreateStore.Handler create,
    UpdateStore.Handler update,
    DeleteStore.Handler delete,
    ListSalesPersonOptions.Handler listSalesPersonOptions,
    TimeProvider clock,
    ILocalizationService localization) : ViewModelBase
{
    private IReadOnlyList<OptionDto> _salesPersonOptions = [];

    public ObservableCollection<StoreDto> Stores { get; } = [];

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RequestDeleteCommand))]
    [NotifyCanExecuteChangedFor(nameof(ConfirmDeleteCommand))]
    public partial StoreDto? SelectedStore { get; set; }

    [ObservableProperty]
    public partial StoreEditorViewModel? Editor { get; set; }

    [ObservableProperty]
    public partial string Search { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string? StatusMessage { get; set; }

    [ObservableProperty]
    public partial bool IsConfirmingDelete { get; set; }

    partial void OnSelectedStoreChanged(StoreDto? value)
    {
        IsConfirmingDelete = false;
        if (value is not null)
        {
            OpenEditor(new StoreEditorViewModel(value, create, update, _salesPersonOptions, clock, localization));
        }
    }

    [RelayCommand]
    private async Task LoadAsync(CancellationToken cancellationToken)
    {
        _salesPersonOptions = await listSalesPersonOptions.HandleAsync(cancellationToken);

        var response = await list.HandleAsync(new ListStores.Query(Search), cancellationToken);

        Stores.Clear();
        foreach (var item in response.Items)
        {
            Stores.Add(item);
        }

        StatusMessage = response.TotalCount > response.Items.Count
            ? localization.Format("Stores_ShowingPartial", response.Items.Count, response.TotalCount)
            : localization.Format("Stores_Count", response.TotalCount);
    }

    [RelayCommand]
    private void New()
    {
        SelectedStore = null;
        OpenEditor(new StoreEditorViewModel(null, create, update, _salesPersonOptions, clock, localization));
    }

    [RelayCommand(CanExecute = nameof(HasSelection))]
    private void RequestDelete() => IsConfirmingDelete = true;

    [RelayCommand]
    private void CancelDelete() => IsConfirmingDelete = false;

    [RelayCommand(CanExecute = nameof(HasSelection))]
    private async Task ConfirmDeleteAsync(CancellationToken cancellationToken)
    {
        if (SelectedStore is not { } selected)
        {
            return;
        }

        var result = await delete.HandleAsync(new DeleteStore.Command(selected.Id), cancellationToken);

        CloseEditor();
        await LoadAsync(cancellationToken);
        if (!result.IsSuccess)
        {
            StatusMessage = localization.Describe(result.Error);
        }
    }

    private bool HasSelection() => SelectedStore is not null;

    private void OpenEditor(StoreEditorViewModel editor)
    {
        DetachEditor();
        Editor = editor;
        editor.Saved += OnEditorSaved;
        editor.Cancelled += OnEditorCancelled;
    }

    private void CloseEditor()
    {
        DetachEditor();
        Editor = null;
        SelectedStore = null;
        IsConfirmingDelete = false;
    }

    private void DetachEditor()
    {
        if (Editor is { } editor)
        {
            editor.Saved -= OnEditorSaved;
            editor.Cancelled -= OnEditorCancelled;
        }
    }

    // Event handler, so it has to be async void; LoadAsync reports its own outcome on the status line.
    private async void OnEditorSaved(object? sender, StoreDto saved)
    {
        CloseEditor();
        await LoadAsync(CancellationToken.None);
        StatusMessage = localization.Get("Stores_Saved");
    }

    private void OnEditorCancelled(object? sender, EventArgs e) => CloseEditor();
}