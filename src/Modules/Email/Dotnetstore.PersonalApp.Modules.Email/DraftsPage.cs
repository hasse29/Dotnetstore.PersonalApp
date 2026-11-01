using Dotnetstore.PersonalApp.Modules.Email.Features.EmailAccounts;
using Dotnetstore.PersonalApp.Modules.Email.Features.Messages;
using Dotnetstore.PersonalApp.Shared.Auth;
using Dotnetstore.PersonalApp.Shared.Files;
using Dotnetstore.PersonalApp.Shared.Localization;
using Dotnetstore.PersonalApp.Shared.Mvvm;
using Dotnetstore.PersonalApp.Shared.Shell;

namespace Dotnetstore.PersonalApp.Modules.Email;

internal sealed class DraftsPage(
    ICurrentUser currentUser,
    ListEmailAccounts.Handler listAccounts,
    SendEmail.Handler send,
    SaveDraft.Handler saveDraft,
    ListDrafts.Handler listDrafts,
    GetDraft.Handler getDraft,
    DeleteDraft.Handler deleteDraft,
    GetEmailAttachment.Handler getAttachment,
    IFilePickService filePicker,
    ILocalizationService localization) : IModulePage
{
    public string? ModuleKey => "email";

    public string Title => Localizer.Instance.Get("Drafts_Title");

    // Material "drafts" icon
    public string IconData => "M21.99 8c0-.72-.37-1.35-.94-1.7L12 1 2.95 6.3C2.38 6.65 2 7.28 2 8v10c0 1.1.9 2 2 2h16c1.1 0 2-.9 2-2l-.01-10zM12 13L3.74 7.84 12 3l8.26 4.84L12 13z";

    public ModulePageGroup? Group => EmailGroup.Create();

    public ViewModelBase CreateViewModel()
    {
        var userId = currentUser.User?.Id ?? Guid.Empty;
        return new DraftsViewModel(
            userId,
            listDrafts,
            getDraft,
            deleteDraft,
            getAttachment,
            () => new ComposeViewModel(userId, isEmbedded: true, listAccounts, send, saveDraft, deleteDraft, filePicker, localization),
            localization);
    }
}
