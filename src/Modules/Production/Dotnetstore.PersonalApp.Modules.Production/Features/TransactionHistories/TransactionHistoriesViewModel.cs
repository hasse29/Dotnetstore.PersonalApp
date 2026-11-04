using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Dotnetstore.PersonalApp.Modules.Production.Features.Products;
using Dotnetstore.PersonalApp.Modules.Production.Features;
using Dotnetstore.PersonalApp.Shared.Localization;
using Dotnetstore.PersonalApp.Shared.Mvvm;

namespace Dotnetstore.PersonalApp.Modules.Production.Features.TransactionHistories;

internal sealed partial class TransactionHistoriesViewModel(
    ListTransactionHistories.Handler list,
    CreateTransactionHistory.Handler create,
    UpdateTransactionHistory.Handler update,
    DeleteTransactionHistory.Handler delete,
    ListProductOptions.Handler listProductOptions,
    TimeProvider clock,
    ILocalizationService localization) : ViewModelBase
{
    private IReadOnlyList<OptionDto> _productOptions = [];

    public ObservableCollection<TransactionHistoryDto> TransactionHistories { get; } = [];

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RequestDeleteCommand))]
    [NotifyCanExecuteChangedFor(nameof(ConfirmDeleteCommand))]
    public partial TransactionHistoryDto? SelectedTransactionHistory { get; set; }

    [ObservableProperty]
    public partial TransactionHistoryEditorViewModel? Editor { get; set; }

    [ObservableProperty]
    public partial string Search { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string? StatusMessage { get; set; }

    [ObservableProperty]
    public partial bool IsConfirmingDelete { get; set; }

    partial void OnSelectedTransactionHistoryChanged(TransactionHistoryDto? value)
    {
        IsConfirmingDelete = false;
        if (value is not null)
        {
            OpenEditor(new TransactionHistoryEditorViewModel(value, create, update, _productOptions, clock, localization));
        }
    }

    [RelayCommand]
    private async Task LoadAsync(CancellationToken cancellationToken)
    {
        _productOptions = await listProductOptions.HandleAsync(cancellationToken);

        var response = await list.HandleAsync(new ListTransactionHistories.Query(Search), cancellationToken);

        TransactionHistories.Clear();
        foreach (var item in response.Items)
        {
            TransactionHistories.Add(item);
        }

        StatusMessage = response.TotalCount > response.Items.Count
            ? localization.Format("TransactionHistories_ShowingPartial", response.Items.Count, response.TotalCount)
            : localization.Format("TransactionHistories_Count", response.TotalCount);
    }

    [RelayCommand]
    private void New()
    {
        SelectedTransactionHistory = null;
        OpenEditor(new TransactionHistoryEditorViewModel(null, create, update, _productOptions, clock, localization));
    }

    [RelayCommand(CanExecute = nameof(HasSelection))]
    private void RequestDelete() => IsConfirmingDelete = true;

    [RelayCommand]
    private void CancelDelete() => IsConfirmingDelete = false;

    [RelayCommand(CanExecute = nameof(HasSelection))]
    private async Task ConfirmDeleteAsync(CancellationToken cancellationToken)
    {
        if (SelectedTransactionHistory is not { } selected)
        {
            return;
        }

        var result = await delete.HandleAsync(new DeleteTransactionHistory.Command(selected.Id), cancellationToken);

        CloseEditor();
        await LoadAsync(cancellationToken);
        if (!result.IsSuccess)
        {
            StatusMessage = localization.Describe(result.Error);
        }
    }

    private bool HasSelection() => SelectedTransactionHistory is not null;

    private void OpenEditor(TransactionHistoryEditorViewModel editor)
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
        SelectedTransactionHistory = null;
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
    private async void OnEditorSaved(object? sender, TransactionHistoryDto saved)
    {
        CloseEditor();
        await LoadAsync(CancellationToken.None);
        StatusMessage = localization.Get("TransactionHistories_Saved");
    }

    private void OnEditorCancelled(object? sender, EventArgs e) => CloseEditor();
}