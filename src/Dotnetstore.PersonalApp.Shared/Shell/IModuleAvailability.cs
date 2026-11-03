namespace Dotnetstore.PersonalApp.Shared.Shell;

/// <summary>
/// Lets a module ask whether a module is switched on, for the parts of the app that are not menu pages (a tab on another module's detail page).
/// Implemented by the host; menu pages are filtered by the shell through <see cref="IModulePage.ModuleKey"/> instead.
/// </summary>
public interface IModuleAvailability
{
    bool IsEnabled(string moduleKey);
}
