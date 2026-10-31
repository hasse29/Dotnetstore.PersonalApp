using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Dotnetstore.PersonalApp.Shared.Addresses;
using Dotnetstore.PersonalApp.Shared.EmailAddresses;
using Dotnetstore.PersonalApp.Shared.Localization;
using Dotnetstore.PersonalApp.Shared.PhoneNumbers;
using Dotnetstore.PersonalApp.Shared.Mvvm;

namespace Dotnetstore.PersonalApp.Modules.Users.Features.Users;

internal sealed partial class UsersViewModel(
    ListUsers.Handler list,
    CreateUser.Handler create,
    UpdateUser.Handler update,
    DeleteUser.Handler delete,
    ILocalizationService localization,
    IObjectAddressesPanelFactory? addressesFactory = null,
    IObjectEmailAddressesPanelFactory? emailAddressesFactory = null,
    IObjectPhoneNumbersPanelFactory? phoneNumbersFactory = null) : ViewModelBase
{
    public ObservableCollection<UserDto> Users { get; } = [];

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(OpenDetailCommand))]
    [NotifyCanExecuteChangedFor(nameof(RequestDeleteCommand))]
    [NotifyCanExecuteChangedFor(nameof(ConfirmDeleteCommand))]
    public partial UserDto? SelectedUser { get; set; }

    [ObservableProperty]
    public partial UserEditorViewModel? Editor { get; set; }

    [ObservableProperty]
    public partial UserDetailViewModel? Detail { get; set; }

    [ObservableProperty]
    public partial string Search { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string? StatusMessage { get; set; }

    [ObservableProperty]
    public partial bool IsConfirmingDelete { get; set; }

    partial void OnSelectedUserChanged(UserDto? value)
    {
        IsConfirmingDelete = false;
        if (value is not null)
        {
            OpenEditor(new UserEditorViewModel(value, create, update, localization));
        }
    }

    [RelayCommand]
    private async Task LoadAsync(CancellationToken cancellationToken)
    {
        var response = await list.HandleAsync(new ListUsers.Query(Search), cancellationToken);

        Users.Clear();
        foreach (var user in response.Items)
        {
            Users.Add(user);
        }

        StatusMessage = response.TotalCount > response.Items.Count
            ? localization.Format("Users_ShowingPartial", response.Items.Count, response.TotalCount)
            : localization.Format("Users_Count", response.TotalCount);
    }

    [RelayCommand]
    private void New()
    {
        SelectedUser = null;
        OpenEditor(new UserEditorViewModel(null, create, update, localization));
    }

    [RelayCommand(CanExecute = nameof(HasSelection))]
    private void OpenDetail()
    {
        if (SelectedUser is not { } user)
        {
            return;
        }

        var detail = new UserDetailViewModel(user, addressesFactory, emailAddressesFactory, phoneNumbersFactory);
        detail.Closed += OnDetailClosed;
        detail.EditRequested += OnDetailClosed;
        Detail = detail;
    }

    // Both Back and Edit return to the list; the selected user's editor is already open there.
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
        if (SelectedUser is not { } user)
        {
            return;
        }

        var result = await delete.HandleAsync(new DeleteUser.Command(user.Id), cancellationToken);

        CloseEditor();
        await LoadAsync(cancellationToken);
        if (!result.IsSuccess)
        {
            StatusMessage = localization.Describe(result.Error);
        }
    }

    private bool HasSelection() => SelectedUser is not null;

    private void OpenEditor(UserEditorViewModel editor)
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
        SelectedUser = null;
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
    private async void OnEditorSaved(object? sender, UserDto saved)
    {
        CloseEditor();
        await LoadAsync(CancellationToken.None);
        StatusMessage = localization.Format("Users_Saved", saved.FullName);
    }

    private void OnEditorCancelled(object? sender, EventArgs e) => CloseEditor();
}
