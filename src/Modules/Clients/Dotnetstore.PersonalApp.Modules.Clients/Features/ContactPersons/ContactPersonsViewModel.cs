using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Dotnetstore.PersonalApp.Shared.Addresses;
using Dotnetstore.PersonalApp.Shared.EmailAddresses;
using Dotnetstore.PersonalApp.Shared.Localization;
using Dotnetstore.PersonalApp.Shared.Mvvm;
using Dotnetstore.PersonalApp.Shared.PhoneNumbers;

namespace Dotnetstore.PersonalApp.Modules.Clients.Features.ContactPersons;

/// <summary>The Contact persons tab of a client: list, editor and a detail page per person.</summary>
internal sealed partial class ContactPersonsViewModel(
    Guid clientId,
    ListContactPersons.Handler list,
    CreateContactPerson.Handler create,
    UpdateContactPerson.Handler update,
    DeleteContactPerson.Handler delete,
    ILocalizationService localization,
    IObjectAddressesPanelFactory? addressesFactory = null,
    IObjectEmailAddressesPanelFactory? emailAddressesFactory = null,
    IObjectPhoneNumbersPanelFactory? phoneNumbersFactory = null) : ViewModelBase
{
    public ObservableCollection<ContactPersonDto> People { get; } = [];

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(OpenDetailCommand))]
    [NotifyCanExecuteChangedFor(nameof(RequestDeleteCommand))]
    [NotifyCanExecuteChangedFor(nameof(ConfirmDeleteCommand))]
    public partial ContactPersonDto? SelectedPerson { get; set; }

    [ObservableProperty]
    public partial ContactPersonEditorViewModel? Editor { get; set; }

    [ObservableProperty]
    public partial ContactPersonDetailViewModel? Detail { get; set; }

    [ObservableProperty]
    public partial string? StatusMessage { get; set; }

    [ObservableProperty]
    public partial bool IsConfirmingDelete { get; set; }

    partial void OnSelectedPersonChanged(ContactPersonDto? value)
    {
        IsConfirmingDelete = false;
        if (value is not null)
        {
            OpenEditor(new ContactPersonEditorViewModel(clientId, value, create, update, localization));
        }
    }

    [RelayCommand]
    private async Task LoadAsync(CancellationToken cancellationToken)
    {
        var people = await list.HandleAsync(new ListContactPersons.Query(clientId), cancellationToken);

        People.Clear();
        foreach (var person in people)
        {
            People.Add(person);
        }

        StatusMessage = localization.Format("ContactPersons_Count", people.Count);
    }

    [RelayCommand]
    private void New()
    {
        SelectedPerson = null;
        OpenEditor(new ContactPersonEditorViewModel(clientId, null, create, update, localization));
    }

    [RelayCommand(CanExecute = nameof(HasSelection))]
    private void OpenDetail()
    {
        if (SelectedPerson is not { } person)
        {
            return;
        }

        var detail = new ContactPersonDetailViewModel(person, addressesFactory, emailAddressesFactory, phoneNumbersFactory);
        detail.Closed += OnDetailClosed;
        Detail = detail;
    }

    private void OnDetailClosed(object? sender, EventArgs e)
    {
        if (Detail is { } detail)
        {
            detail.Closed -= OnDetailClosed;
        }

        Detail = null;
    }

    [RelayCommand(CanExecute = nameof(HasSelection))]
    private void RequestDelete() => IsConfirmingDelete = true;

    [RelayCommand]
    private void CancelDelete() => IsConfirmingDelete = false;

    [RelayCommand(CanExecute = nameof(HasSelection))]
    private async Task ConfirmDeleteAsync(CancellationToken cancellationToken)
    {
        if (SelectedPerson is not { } person)
        {
            return;
        }

        var result = await delete.HandleAsync(new DeleteContactPerson.Command(person.Id), cancellationToken);

        CloseEditor();
        await LoadAsync(cancellationToken);
        if (!result.IsSuccess)
        {
            StatusMessage = localization.Describe(result.Error);
        }
    }

    private bool HasSelection() => SelectedPerson is not null;

    private void OpenEditor(ContactPersonEditorViewModel editor)
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
        SelectedPerson = null;
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
    private async void OnEditorSaved(object? sender, ContactPersonDto saved)
    {
        CloseEditor();
        await LoadAsync(CancellationToken.None);
        StatusMessage = localization.Format("ContactPersons_Saved", saved.FullName);
    }

    private void OnEditorCancelled(object? sender, EventArgs e) => CloseEditor();
}
