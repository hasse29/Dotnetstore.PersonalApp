using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Dotnetstore.PersonalApp.Modules.Email.Domain;
using Dotnetstore.PersonalApp.Shared.Localization;
using Dotnetstore.PersonalApp.Shared.Mvvm;

namespace Dotnetstore.PersonalApp.Modules.Email.Features.EmailAccounts;

/// <summary>The accounts the signed-in user can use: their own and the system accounts.</summary>
internal sealed partial class EmailAccountsViewModel(
    Guid userId,
    ListEmailAccounts.Handler list,
    CreateEmailAccount.Handler create,
    UpdateEmailAccount.Handler update,
    TestEmailConnection.Handler test,
    DeleteEmailAccount.Handler delete,
    ILocalizationService localization) : ViewModelBase
{
    public ObservableCollection<EmailAccountRow> Accounts { get; } = [];

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RequestDeleteCommand))]
    [NotifyCanExecuteChangedFor(nameof(ConfirmDeleteCommand))]
    public partial EmailAccountRow? SelectedAccount { get; set; }

    [ObservableProperty]
    public partial EmailAccountEditorViewModel? Editor { get; set; }

    [ObservableProperty]
    public partial string? StatusMessage { get; set; }

    [ObservableProperty]
    public partial bool IsConfirmingDelete { get; set; }

    partial void OnSelectedAccountChanged(EmailAccountRow? value)
    {
        IsConfirmingDelete = false;
        if (value is not null)
        {
            OpenEditor(new EmailAccountEditorViewModel(value.Account, userId, create, update, test, localization));
        }
    }

    [RelayCommand]
    private async Task LoadAsync(CancellationToken cancellationToken)
    {
        var response = await list.HandleAsync(new ListEmailAccounts.Query(userId), cancellationToken);

        Accounts.Clear();
        foreach (var account in response.Items)
        {
            Accounts.Add(ToRow(account));
        }

        StatusMessage = response.TotalCount > response.Items.Count
            ? localization.Format("Email_ShowingPartial", response.Items.Count, response.TotalCount)
            : localization.Format("Email_Count", response.TotalCount);
    }

    [RelayCommand]
    private void New()
    {
        SelectedAccount = null;
        OpenEditor(new EmailAccountEditorViewModel(null, userId, create, update, test, localization));
    }

    [RelayCommand(CanExecute = nameof(HasSelection))]
    private void RequestDelete() => IsConfirmingDelete = true;

    [RelayCommand]
    private void CancelDelete() => IsConfirmingDelete = false;

    [RelayCommand(CanExecute = nameof(HasSelection))]
    private async Task ConfirmDeleteAsync(CancellationToken cancellationToken)
    {
        if (SelectedAccount is not { } row)
        {
            return;
        }

        var result = await delete.HandleAsync(new DeleteEmailAccount.Command(row.Account.Id, userId), cancellationToken);

        CloseEditor();
        await LoadAsync(cancellationToken);
        if (!result.IsSuccess)
        {
            StatusMessage = localization.Describe(result.Error);
        }
    }

    private bool HasSelection() => SelectedAccount is not null;

    private EmailAccountRow ToRow(EmailAccountDto account) => new(
        account,
        localization.Get(account.Protocol == MailProtocol.Smtp ? "Email_ProtocolSmtp" : "Email_ProtocolPop3"),
        string.Create(CultureInfo.InvariantCulture, $"{account.Host}:{account.Port}"),
        localization.Get(account.IsSystem ? "Email_ScopeSystem" : "Email_ScopePersonal"));

    private void OpenEditor(EmailAccountEditorViewModel editor)
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
        SelectedAccount = null;
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
    private async void OnEditorSaved(object? sender, EmailAccountDto saved)
    {
        CloseEditor();
        await LoadAsync(CancellationToken.None);
        StatusMessage = localization.Format("Email_Saved", saved.Name);
    }

    private void OnEditorCancelled(object? sender, EventArgs e) => CloseEditor();
}
