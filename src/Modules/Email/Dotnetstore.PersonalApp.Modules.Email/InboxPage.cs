using Dotnetstore.PersonalApp.Modules.Email.Features.EmailAccounts;
using Dotnetstore.PersonalApp.Modules.Email.Features.Messages;
using Dotnetstore.PersonalApp.Shared.Auth;
using Dotnetstore.PersonalApp.Shared.Files;
using Dotnetstore.PersonalApp.Shared.Localization;
using Dotnetstore.PersonalApp.Shared.Mvvm;
using Dotnetstore.PersonalApp.Shared.Shell;

namespace Dotnetstore.PersonalApp.Modules.Email;

internal sealed class InboxPage(
    ICurrentUser currentUser,
    ListEmailAccounts.Handler listAccounts,
    FetchEmail.Handler fetch,
    ListEmailMessages.Handler listMessages,
    GetEmailMessage.Handler getMessage,
    DeleteEmailMessage.Handler deleteMessage,
    GetEmailAttachment.Handler getAttachment,
    SendEmail.Handler send,
    SaveDraft.Handler saveDraft,
    DeleteDraft.Handler deleteDraft,
    IFileSaveService fileSave,
    IFilePickService filePicker,
    ILocalizationService localization) : IModulePage
{
    public string? ModuleKey => "email";

    public string Title => Localizer.Instance.Get("Inbox_Title");

    // Material "inbox" icon
    public string IconData => "M19 3H4.99c-1.11 0-1.98.89-1.98 2L3 19c0 1.1.88 2 1.99 2H19c1.1 0 2-.9 2-2V5c0-1.11-.9-2-2-2zm0 12h-4c0 1.66-1.35 3-3 3s-3-1.34-3-3H4.99V5H19v10z";

    public ModulePageGroup? Group => EmailGroup.Create();

    public ViewModelBase CreateViewModel()
    {
        var userId = currentUser.User?.Id ?? Guid.Empty;
        return new InboxViewModel(
            userId,
            listAccounts,
            fetch,
            listMessages,
            getMessage,
            deleteMessage,
            getAttachment,
            fileSave,
            () => new ComposeViewModel(userId, isEmbedded: true, listAccounts, send, saveDraft, deleteDraft, filePicker, localization),
            localization);
    }
}
