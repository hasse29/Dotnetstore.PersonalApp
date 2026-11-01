using Dotnetstore.PersonalApp.Modules.Email.Features.EmailAccounts;
using Dotnetstore.PersonalApp.Modules.Email.Features.Messages;
using Dotnetstore.PersonalApp.Shared.Auth;
using Dotnetstore.PersonalApp.Shared.Files;
using Dotnetstore.PersonalApp.Shared.Localization;
using Dotnetstore.PersonalApp.Shared.Mvvm;
using Dotnetstore.PersonalApp.Shared.Shell;

namespace Dotnetstore.PersonalApp.Modules.Email;

internal sealed class SentPage(
    ICurrentUser currentUser,
    ListEmailAccounts.Handler listAccounts,
    ListSentMessages.Handler listMessages,
    GetSentMessage.Handler getMessage,
    DeleteSentMessage.Handler deleteMessage,
    GetEmailAttachment.Handler getAttachment,
    IFileSaveService fileSave,
    ILocalizationService localization) : IModulePage
{
    public string? ModuleKey => "email";

    public string Title => Localizer.Instance.Get("Sent_Title");

    // Material "send" icon
    public string IconData => "M2.01 21L23 12 2.01 3 2 10l15 2-15 2z";

    public ModulePageGroup? Group => EmailGroup.Create();

    public ViewModelBase CreateViewModel() =>
        new SentViewModel(currentUser.User?.Id ?? Guid.Empty, listAccounts, listMessages, getMessage, deleteMessage, getAttachment, fileSave, localization);
}
