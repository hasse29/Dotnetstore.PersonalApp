using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Dotnetstore.PersonalApp.Modules.Contact.EmailAddresses.Features.EmailAddressTypes;
using Dotnetstore.PersonalApp.Shared.Localization;
using Dotnetstore.PersonalApp.Shared.Mvvm;

namespace Dotnetstore.PersonalApp.Modules.Contact.EmailAddresses.Features.EmailAddresses;

/// <summary>The addresses of one object (e.g. a user), shown as a tab on that object's detail page.</summary>
internal sealed partial class ObjectEmailAddressesViewModel(
    Guid objectId,
    ListEmailAddresses.Handler list,
    ListEmailAddressTypes.Handler listTypes,
    CreateEmailAddress.Handler create,
    UpdateEmailAddress.Handler update,
    DeleteEmailAddress.Handler delete,
    DeleteEmailAddressesForObject.Handler deleteAll,
    ILocalizationService localization) : ViewModelBase
{
    private IReadOnlyList<EmailAddressTypeDto> _types = [];

    public ObservableCollection<EmailAddressDto> EmailAddresses { get; } = [];

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RequestDeleteCommand))]
    [NotifyCanExecuteChangedFor(nameof(ConfirmDeleteCommand))]
    public partial EmailAddressDto? SelectedEmailAddress { get; set; }

    [ObservableProperty]
    public partial EmailAddressEditorViewModel? Editor { get; set; }

    [ObservableProperty]
    public partial string? StatusMessage { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsConfirmingDelete), nameof(IsConfirmingDeleteAll), nameof(IsIdle))]
    public partial DeleteMode Mode { get; set; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RequestDeleteAllCommand))]
    public partial bool HasEmailAddresses { get; set; }

    public bool IsConfirmingDelete => Mode == DeleteMode.One;

    public bool IsConfirmingDeleteAll => Mode == DeleteMode.All;

    public bool IsIdle => Mode == DeleteMode.None;

    internal enum DeleteMode
    {
        None,
        One,
        All,
    }

    partial void OnSelectedEmailAddressChanged(EmailAddressDto? value)
    {
        Mode = DeleteMode.None;
        if (value is not null)
        {
            OpenEditor(new EmailAddressEditorViewModel(value, objectId, _types, create, update, localization));
        }
    }

    [RelayCommand]
    private async Task LoadAsync(CancellationToken cancellationToken)
    {
        _types = (await listTypes.HandleAsync(new ListEmailAddressTypes.Query(), cancellationToken)).Items;
        var response = await list.HandleAsync(new ListEmailAddresses.Query(objectId), cancellationToken);

        EmailAddresses.Clear();
        foreach (var address in response.Items)
        {
            EmailAddresses.Add(address);
        }

        HasEmailAddresses = EmailAddresses.Count > 0;
        StatusMessage = localization.Format("EmailAddressesPanel_Count", response.TotalCount);
    }

    [RelayCommand]
    private void New()
    {
        if (_types.Count == 0)
        {
            StatusMessage = localization.Get("EmailAddressesPanel_NoTypes");
            return;
        }

        SelectedEmailAddress = null;
        OpenEditor(new EmailAddressEditorViewModel(null, objectId, _types, create, update, localization));
    }

    [RelayCommand(CanExecute = nameof(HasSelection))]
    private void RequestDelete() => Mode = DeleteMode.One;

    [RelayCommand(CanExecute = nameof(HasEmailAddresses))]
    private void RequestDeleteAll() => Mode = DeleteMode.All;

    [RelayCommand]
    private void CancelDelete() => Mode = DeleteMode.None;

    [RelayCommand(CanExecute = nameof(HasSelection))]
    private async Task ConfirmDeleteAsync(CancellationToken cancellationToken)
    {
        if (SelectedEmailAddress is not { } address)
        {
            return;
        }

        var result = await delete.HandleAsync(new DeleteEmailAddress.Command(address.Id), cancellationToken);

        CloseEditor();
        await LoadAsync(cancellationToken);
        if (!result.IsSuccess)
        {
            StatusMessage = localization.Describe(result.Error);
        }
    }

    [RelayCommand]
    private async Task ConfirmDeleteAllAsync(CancellationToken cancellationToken)
    {
        var result = await deleteAll.HandleAsync(new DeleteEmailAddressesForObject.Command(objectId), cancellationToken);

        CloseEditor();
        await LoadAsync(cancellationToken);
        StatusMessage = result.IsSuccess
            ? localization.Format("EmailAddressesPanel_Deleted", result.Value)
            : localization.Describe(result.Error);
    }

    private bool HasSelection() => SelectedEmailAddress is not null;

    private void OpenEditor(EmailAddressEditorViewModel editor)
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
        SelectedEmailAddress = null;
        Mode = DeleteMode.None;
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
    private async void OnEditorSaved(object? sender, EmailAddressDto saved)
    {
        CloseEditor();
        await LoadAsync(CancellationToken.None);
        StatusMessage = localization.Format("EmailAddressesPanel_Saved", saved.EmailAddressTypeName);
    }

    private void OnEditorCancelled(object? sender, EventArgs e) => CloseEditor();
}
