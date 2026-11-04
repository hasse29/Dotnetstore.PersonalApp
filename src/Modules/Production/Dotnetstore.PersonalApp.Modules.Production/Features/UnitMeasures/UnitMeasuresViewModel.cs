using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Dotnetstore.PersonalApp.Modules.Production.Features;
using Dotnetstore.PersonalApp.Shared.Localization;
using Dotnetstore.PersonalApp.Shared.Mvvm;

namespace Dotnetstore.PersonalApp.Modules.Production.Features.UnitMeasures;

internal sealed partial class UnitMeasuresViewModel(
    ListUnitMeasures.Handler list,
    CreateUnitMeasure.Handler create,
    UpdateUnitMeasure.Handler update,
    DeleteUnitMeasure.Handler delete,
    TimeProvider clock,
    ILocalizationService localization) : ViewModelBase
{
    public ObservableCollection<UnitMeasureDto> UnitMeasures { get; } = [];

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RequestDeleteCommand))]
    [NotifyCanExecuteChangedFor(nameof(ConfirmDeleteCommand))]
    public partial UnitMeasureDto? SelectedUnitMeasure { get; set; }

    [ObservableProperty]
    public partial UnitMeasureEditorViewModel? Editor { get; set; }

    [ObservableProperty]
    public partial string Search { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string? StatusMessage { get; set; }

    [ObservableProperty]
    public partial bool IsConfirmingDelete { get; set; }

    partial void OnSelectedUnitMeasureChanged(UnitMeasureDto? value)
    {
        IsConfirmingDelete = false;
        if (value is not null)
        {
            OpenEditor(new UnitMeasureEditorViewModel(value, create, update, clock, localization));
        }
    }

    [RelayCommand]
    private async Task LoadAsync(CancellationToken cancellationToken)
    {
        var response = await list.HandleAsync(new ListUnitMeasures.Query(Search), cancellationToken);

        UnitMeasures.Clear();
        foreach (var item in response.Items)
        {
            UnitMeasures.Add(item);
        }

        StatusMessage = response.TotalCount > response.Items.Count
            ? localization.Format("UnitMeasures_ShowingPartial", response.Items.Count, response.TotalCount)
            : localization.Format("UnitMeasures_Count", response.TotalCount);
    }

    [RelayCommand]
    private void New()
    {
        SelectedUnitMeasure = null;
        OpenEditor(new UnitMeasureEditorViewModel(null, create, update, clock, localization));
    }

    [RelayCommand(CanExecute = nameof(HasSelection))]
    private void RequestDelete() => IsConfirmingDelete = true;

    [RelayCommand]
    private void CancelDelete() => IsConfirmingDelete = false;

    [RelayCommand(CanExecute = nameof(HasSelection))]
    private async Task ConfirmDeleteAsync(CancellationToken cancellationToken)
    {
        if (SelectedUnitMeasure is not { } selected)
        {
            return;
        }

        var result = await delete.HandleAsync(new DeleteUnitMeasure.Command(selected.Id), cancellationToken);

        CloseEditor();
        await LoadAsync(cancellationToken);
        if (!result.IsSuccess)
        {
            StatusMessage = localization.Describe(result.Error);
        }
    }

    private bool HasSelection() => SelectedUnitMeasure is not null;

    private void OpenEditor(UnitMeasureEditorViewModel editor)
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
        SelectedUnitMeasure = null;
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
    private async void OnEditorSaved(object? sender, UnitMeasureDto saved)
    {
        CloseEditor();
        await LoadAsync(CancellationToken.None);
        StatusMessage = localization.Get("UnitMeasures_Saved");
    }

    private void OnEditorCancelled(object? sender, EventArgs e) => CloseEditor();
}