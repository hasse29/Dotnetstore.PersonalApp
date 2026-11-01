using Dotnetstore.PersonalApp.Shared.Localization;
using Dotnetstore.PersonalApp.Shared.Shell;

namespace Dotnetstore.PersonalApp.Modules.Email;

/// <summary>The "Email" navigation group that the module's pages (inbox, compose, accounts) share.</summary>
internal static class EmailGroup
{
    // Material "email" icon
    public const string IconData = "M20 4H4c-1.1 0-1.99.9-1.99 2L2 18c0 1.1.9 2 2 2h16c1.1 0 2-.9 2-2V6c0-1.1-.9-2-2-2zm0 4l-8 5-8-5V6l8 5 8-5v2z";

    public static ModulePageGroup Create() => new("email", Localizer.Instance.Get("Email_Module"), IconData);
}
