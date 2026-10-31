using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Dotnetstore.PersonalApp.Shared.Localization;
using Dotnetstore.PersonalApp.Shared.Mvvm;

namespace Dotnetstore.PersonalApp.Modules.EmailAddresses.Features.EmailAddressTypes;

internal sealed partial class EmailAddressTypesViewModel(
    ListEmailAddressTypes.Handler list,
    CreateEmailAddressType.Handler create,
    UpdateEmailAddressType.Handler update,
    DeleteEmailAddressType.Handler delete,
    ILocalizationService localization) : ViewModelBase
{
    public ObservableCollection<EmailAddressTypeDto> EmailAddressTypes { get; } = [];

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RequestDeleteCommand))]
    [NotifyCanExecuteChangedFor(nameof(ConfirmDeleteCommand))]
    public partial EmailAddressTypeDto? SelectedType { get; set; }

    [ObservableProperty]
    public partial EmailAddressTypeEditorViewModel? Editor { get; set; }

    [ObservableProperty]
    public partial string Search { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string? StatusMessage { get; set; }

    [ObservableProperty]
    public partial bool IsConfirmingDelete { get; set; }

    partial void OnSelectedTypeChanged(EmailAddressTypeDto? value)
    {
        IsConfirmingDelete = false;
        if (value is not null)
        {
            OpenEditor(new EmailAddressTypeEditorViewModel(value, create, update, localization));
        }
    }

    [RelayCommand]
    private async Task LoadAsync(CancellationToken cancellationToken)
    {
        var response = await list.HandleAsync(new ListEmailAddressTypes.Query(Search), cancellationToken);

        EmailAddressTypes.Clear();
        foreach (var type in response.Items)
        {
            EmailAddressTypes.Add(type);
        }

        StatusMessage = response.TotalCount > response.Items.Count
            ? localization.Format("EmailAddressTypes_ShowingPartial", response.Items.Count, response.TotalCount)
            : localization.Format("EmailAddressTypes_Count", response.TotalCount);
    }

    [RelayCommand]
    private void New()
    {
        SelectedType = null;
        OpenEditor(new EmailAddressTypeEditorViewModel(null, create, update, localization));
    }

    [RelayCommand(CanExecute = nameof(HasSelection))]
    private void RequestDelete() => IsConfirmingDelete = true;

    [RelayCommand]
    private void CancelDelete() => IsConfirmingDelete = false;

    [RelayCommand(CanExecute = nameof(HasSelection))]
    private async Task ConfirmDeleteAsync(CancellationToken cancellationToken)
    {
        if (SelectedType is not { } type)
        {
            return;
        }

        var result = await delete.HandleAsync(new DeleteEmailAddressType.Command(type.Id), cancellationToken);

        CloseEditor();
        await LoadAsync(cancellationToken);
        if (!result.IsSuccess)
        {
            StatusMessage = localization.Describe(result.Error);
        }
    }

    private bool HasSelection() => SelectedType is not null;

    private void OpenEditor(EmailAddressTypeEditorViewModel editor)
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
        SelectedType = null;
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
    private async void OnEditorSaved(object? sender, EmailAddressTypeDto saved)
    {
        CloseEditor();
        await LoadAsync(CancellationToken.None);
        StatusMessage = localization.Format("EmailAddressTypes_Saved", saved.Name);
    }

    private void OnEditorCancelled(object? sender, EventArgs e) => CloseEditor();
}
