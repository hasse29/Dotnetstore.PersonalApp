using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Dotnetstore.PersonalApp.Modules.Sales.Features;
using Dotnetstore.PersonalApp.Shared.Localization;
using Dotnetstore.PersonalApp.Shared.Mvvm;

namespace Dotnetstore.PersonalApp.Modules.Sales.Features.SpecialOffers;

internal sealed partial class SpecialOffersViewModel(
    ListSpecialOffers.Handler list,
    CreateSpecialOffer.Handler create,
    UpdateSpecialOffer.Handler update,
    DeleteSpecialOffer.Handler delete,
    TimeProvider clock,
    ILocalizationService localization) : ViewModelBase
{
    public ObservableCollection<SpecialOfferDto> SpecialOffers { get; } = [];

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RequestDeleteCommand))]
    [NotifyCanExecuteChangedFor(nameof(ConfirmDeleteCommand))]
    public partial SpecialOfferDto? SelectedSpecialOffer { get; set; }

    [ObservableProperty]
    public partial SpecialOfferEditorViewModel? Editor { get; set; }

    [ObservableProperty]
    public partial string Search { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string? StatusMessage { get; set; }

    [ObservableProperty]
    public partial bool IsConfirmingDelete { get; set; }

    partial void OnSelectedSpecialOfferChanged(SpecialOfferDto? value)
    {
        IsConfirmingDelete = false;
        if (value is not null)
        {
            OpenEditor(new SpecialOfferEditorViewModel(value, create, update, clock, localization));
        }
    }

    [RelayCommand]
    private async Task LoadAsync(CancellationToken cancellationToken)
    {
        var response = await list.HandleAsync(new ListSpecialOffers.Query(Search), cancellationToken);

        SpecialOffers.Clear();
        foreach (var item in response.Items)
        {
            SpecialOffers.Add(item);
        }

        StatusMessage = response.TotalCount > response.Items.Count
            ? localization.Format("SpecialOffers_ShowingPartial", response.Items.Count, response.TotalCount)
            : localization.Format("SpecialOffers_Count", response.TotalCount);
    }

    [RelayCommand]
    private void New()
    {
        SelectedSpecialOffer = null;
        OpenEditor(new SpecialOfferEditorViewModel(null, create, update, clock, localization));
    }

    [RelayCommand(CanExecute = nameof(HasSelection))]
    private void RequestDelete() => IsConfirmingDelete = true;

    [RelayCommand]
    private void CancelDelete() => IsConfirmingDelete = false;

    [RelayCommand(CanExecute = nameof(HasSelection))]
    private async Task ConfirmDeleteAsync(CancellationToken cancellationToken)
    {
        if (SelectedSpecialOffer is not { } selected)
        {
            return;
        }

        var result = await delete.HandleAsync(new DeleteSpecialOffer.Command(selected.Id), cancellationToken);

        CloseEditor();
        await LoadAsync(cancellationToken);
        if (!result.IsSuccess)
        {
            StatusMessage = localization.Describe(result.Error);
        }
    }

    private bool HasSelection() => SelectedSpecialOffer is not null;

    private void OpenEditor(SpecialOfferEditorViewModel editor)
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
        SelectedSpecialOffer = null;
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
    private async void OnEditorSaved(object? sender, SpecialOfferDto saved)
    {
        CloseEditor();
        await LoadAsync(CancellationToken.None);
        StatusMessage = localization.Get("SpecialOffers_Saved");
    }

    private void OnEditorCancelled(object? sender, EventArgs e) => CloseEditor();
}