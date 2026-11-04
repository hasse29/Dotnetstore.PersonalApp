using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Dotnetstore.PersonalApp.Modules.Sales.Features;
using Dotnetstore.PersonalApp.Shared.Localization;
using Dotnetstore.PersonalApp.Shared.Mvvm;

namespace Dotnetstore.PersonalApp.Modules.Sales.Features.SalesReasons;

internal sealed partial class SalesReasonsViewModel(
    ListSalesReasons.Handler list,
    CreateSalesReason.Handler create,
    UpdateSalesReason.Handler update,
    DeleteSalesReason.Handler delete,
    TimeProvider clock,
    ILocalizationService localization) : ViewModelBase
{
    public ObservableCollection<SalesReasonDto> SalesReasons { get; } = [];

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RequestDeleteCommand))]
    [NotifyCanExecuteChangedFor(nameof(ConfirmDeleteCommand))]
    public partial SalesReasonDto? SelectedSalesReason { get; set; }

    [ObservableProperty]
    public partial SalesReasonEditorViewModel? Editor { get; set; }

    [ObservableProperty]
    public partial string Search { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string? StatusMessage { get; set; }

    [ObservableProperty]
    public partial bool IsConfirmingDelete { get; set; }

    partial void OnSelectedSalesReasonChanged(SalesReasonDto? value)
    {
        IsConfirmingDelete = false;
        if (value is not null)
        {
            OpenEditor(new SalesReasonEditorViewModel(value, create, update, clock, localization));
        }
    }

    [RelayCommand]
    private async Task LoadAsync(CancellationToken cancellationToken)
    {
        var response = await list.HandleAsync(new ListSalesReasons.Query(Search), cancellationToken);

        SalesReasons.Clear();
        foreach (var item in response.Items)
        {
            SalesReasons.Add(item);
        }

        StatusMessage = response.TotalCount > response.Items.Count
            ? localization.Format("SalesReasons_ShowingPartial", response.Items.Count, response.TotalCount)
            : localization.Format("SalesReasons_Count", response.TotalCount);
    }

    [RelayCommand]
    private void New()
    {
        SelectedSalesReason = null;
        OpenEditor(new SalesReasonEditorViewModel(null, create, update, clock, localization));
    }

    [RelayCommand(CanExecute = nameof(HasSelection))]
    private void RequestDelete() => IsConfirmingDelete = true;

    [RelayCommand]
    private void CancelDelete() => IsConfirmingDelete = false;

    [RelayCommand(CanExecute = nameof(HasSelection))]
    private async Task ConfirmDeleteAsync(CancellationToken cancellationToken)
    {
        if (SelectedSalesReason is not { } selected)
        {
            return;
        }

        var result = await delete.HandleAsync(new DeleteSalesReason.Command(selected.Id), cancellationToken);

        CloseEditor();
        await LoadAsync(cancellationToken);
        if (!result.IsSuccess)
        {
            StatusMessage = localization.Describe(result.Error);
        }
    }

    private bool HasSelection() => SelectedSalesReason is not null;

    private void OpenEditor(SalesReasonEditorViewModel editor)
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
        SelectedSalesReason = null;
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
    private async void OnEditorSaved(object? sender, SalesReasonDto saved)
    {
        CloseEditor();
        await LoadAsync(CancellationToken.None);
        StatusMessage = localization.Get("SalesReasons_Saved");
    }

    private void OnEditorCancelled(object? sender, EventArgs e) => CloseEditor();
}