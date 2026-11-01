using Dotnetstore.PersonalApp.Modules.Email.Features.EmailAccounts;
using Dotnetstore.PersonalApp.Modules.Email.Features.Messages;
using Dotnetstore.PersonalApp.Shared.Auth;
using Dotnetstore.PersonalApp.Shared.Files;
using Dotnetstore.PersonalApp.Shared.Localization;
using Dotnetstore.PersonalApp.Shared.Mvvm;
using Dotnetstore.PersonalApp.Shared.Shell;

namespace Dotnetstore.PersonalApp.Modules.Email;

internal sealed class ComposePage(
    ICurrentUser currentUser,
    ListEmailAccounts.Handler listAccounts,
    SendEmail.Handler send,
    SaveDraft.Handler saveDraft,
    DeleteDraft.Handler deleteDraft,
    IFilePickService filePicker,
    ILocalizationService localization) : IModulePage
{
    public string? ModuleKey => "email";

    public string Title => Localizer.Instance.Get("Compose_Title");

    // Material "edit" icon
    public string IconData => "M3 17.25V21h3.75L17.81 9.94l-3.75-3.75L3 17.25zM20.71 7.04c.39-.39.39-1.02 0-1.41l-2.34-2.34a.9959.9959 0 0 0-1.41 0l-1.83 1.83 3.75 3.75 1.83-1.83z";

    public ModulePageGroup? Group => EmailGroup.Create();

    public ViewModelBase CreateViewModel() =>
        new ComposeViewModel(currentUser.User?.Id ?? Guid.Empty, isEmbedded: false, listAccounts, send, saveDraft, deleteDraft, filePicker, localization);
}
