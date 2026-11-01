using Dotnetstore.PersonalApp.Modules.Email.Features.EmailAccounts;
using Dotnetstore.PersonalApp.Shared.Auth;
using Dotnetstore.PersonalApp.Shared.Localization;
using Dotnetstore.PersonalApp.Shared.Mvvm;
using Dotnetstore.PersonalApp.Shared.Shell;

namespace Dotnetstore.PersonalApp.Modules.Email;

internal sealed class EmailAccountsPage(
    ICurrentUser currentUser,
    ListEmailAccounts.Handler list,
    CreateEmailAccount.Handler create,
    UpdateEmailAccount.Handler update,
    TestEmailConnection.Handler test,
    DeleteEmailAccount.Handler delete,
    ILocalizationService localization) : IModulePage
{
    public string? ModuleKey => "email";

    public string Title => Localizer.Instance.Get("Email_Title");

    // Material "email" icon
    public string IconData =>
        "M20 4H4c-1.1 0-1.99.9-1.99 2L2 18c0 1.1.9 2 2 2h16c1.1 0 2-.9 2-2V6c0-1.1-.9-2-2-2zm0 4l-8 5-8-5V6l8 5 8-5v2z";

    public ModulePageGroup? Group => EmailGroup.Create();

    /// <summary>With nobody signed in the list shows only the system accounts, and adding is rejected by the handlers.</summary>
    public ViewModelBase CreateViewModel() =>
        new EmailAccountsViewModel(currentUser.User?.Id ?? Guid.Empty, list, create, update, test, delete, localization);
}
