using Dotnetstore.PersonalApp.Modules.Contact.EmailAddresses.Features.EmailAddressTypes;
using Dotnetstore.PersonalApp.Shared.Localization;
using Dotnetstore.PersonalApp.Shared.Mvvm;
using Dotnetstore.PersonalApp.Shared.Shell;
using Microsoft.Extensions.DependencyInjection;

namespace Dotnetstore.PersonalApp.Modules.Contact.EmailAddresses;

/// <summary>The EmailAddresses module exposes email address type management here; the email addresses themselves are managed on the owners' detail pages.</summary>
internal sealed class EmailAddressesPage(IServiceProvider services) : IModulePage
{
    public string? ModuleKey => ContactModule.Key;

    public string Title => Localizer.Instance.Get("EmailAddressTypes_Title");

    // Material "email" icon
    public string IconData =>
        "M20 4H4c-1.1 0-1.99.9-1.99 2L2 18c0 1.1.9 2 2 2h16c1.1 0 2-.9 2-2V6c0-1.1-.9-2-2-2zm0 4l-8 5-8-5V6l8 5 8-5v2z";

    public ModulePageGroup? Group => SettingsGroup.Create();

    public ViewModelBase CreateViewModel() => services.GetRequiredService<EmailAddressTypesViewModel>();
}
