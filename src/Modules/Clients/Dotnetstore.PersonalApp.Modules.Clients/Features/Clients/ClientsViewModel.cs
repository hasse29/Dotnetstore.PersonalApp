using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Dotnetstore.PersonalApp.Shared.Addresses;
using Dotnetstore.PersonalApp.Shared.EmailAddresses;
using Dotnetstore.PersonalApp.Shared.Localization;
using Dotnetstore.PersonalApp.Shared.PhoneNumbers;
using Dotnetstore.PersonalApp.Shared.TimeReports;
using Dotnetstore.PersonalApp.Shared.Mvvm;

namespace Dotnetstore.PersonalApp.Modules.Clients.Features.Clients;

internal sealed partial class ClientsViewModel(
    ListClients.Handler list,
    CreateClient.Handler create,
    UpdateClient.Handler update,
    DeleteClient.Handler delete,
    ILocalizationService localization,
    IObjectAddressesPanelFactory? addressesFactory = null,
    IObjectEmailAddressesPanelFactory? emailAddressesFactory = null,
    IObjectPhoneNumbersPanelFactory? phoneNumbersFactory = null,
    ITimeReportsPanelFactory? timeReportsFactory = null) : ViewModelBase
{
    public ObservableCollection<ClientDto> Clients { get; } = [];

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(OpenDetailCommand))]
    [NotifyCanExecuteChangedFor(nameof(RequestDeleteCommand))]
    [NotifyCanExecuteChangedFor(nameof(ConfirmDeleteCommand))]
    public partial ClientDto? SelectedClient { get; set; }

    [ObservableProperty]
    public partial ClientEditorViewModel? Editor { get; set; }

    [ObservableProperty]
    public partial ClientDetailViewModel? Detail { get; set; }

    [ObservableProperty]
    public partial string Search { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string? StatusMessage { get; set; }

    [ObservableProperty]
    public partial bool IsConfirmingDelete { get; set; }

    partial void OnSelectedClientChanged(ClientDto? value)
    {
        IsConfirmingDelete = false;
        if (value is not null)
        {
            OpenEditor(new ClientEditorViewModel(value, create, update, localization));
        }
    }

    [RelayCommand]
    private async Task LoadAsync(CancellationToken cancellationToken)
    {
        var response = await list.HandleAsync(new ListClients.Query(Search), cancellationToken);

        Clients.Clear();
        foreach (var client in response.Items)
        {
            Clients.Add(client);
        }

        StatusMessage = response.TotalCount > response.Items.Count
            ? localization.Format("Clients_ShowingPartial", response.Items.Count, response.TotalCount)
            : localization.Format("Clients_Count", response.TotalCount);
    }

    [RelayCommand]
    private void New()
    {
        SelectedClient = null;
        OpenEditor(new ClientEditorViewModel(null, create, update, localization));
    }

    [RelayCommand(CanExecute = nameof(HasSelection))]
    private void OpenDetail()
    {
        if (SelectedClient is not { } client)
        {
            return;
        }

        var detail = new ClientDetailViewModel(client, addressesFactory, emailAddressesFactory, phoneNumbersFactory, timeReportsFactory);
        detail.Closed += OnDetailClosed;
        detail.EditRequested += OnDetailClosed;
        Detail = detail;
    }

    // Both Back and Edit return to the list; the selected client's editor is already open there.
    private void OnDetailClosed(object? sender, EventArgs e)
    {
        if (Detail is { } detail)
        {
            detail.Closed -= OnDetailClosed;
            detail.EditRequested -= OnDetailClosed;
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
        if (SelectedClient is not { } client)
        {
            return;
        }

        var result = await delete.HandleAsync(new DeleteClient.Command(client.Id), cancellationToken);

        CloseEditor();
        await LoadAsync(cancellationToken);
        if (!result.IsSuccess)
        {
            StatusMessage = localization.Describe(result.Error);
        }
    }

    private bool HasSelection() => SelectedClient is not null;

    private void OpenEditor(ClientEditorViewModel editor)
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
        SelectedClient = null;
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
    private async void OnEditorSaved(object? sender, ClientDto saved)
    {
        CloseEditor();
        await LoadAsync(CancellationToken.None);
        StatusMessage = localization.Format("Clients_Saved", saved.Name);
    }

    private void OnEditorCancelled(object? sender, EventArgs e) => CloseEditor();
}
