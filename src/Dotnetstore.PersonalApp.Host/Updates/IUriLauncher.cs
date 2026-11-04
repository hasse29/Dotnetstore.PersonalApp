namespace Dotnetstore.PersonalApp.Host.Updates;

/// <summary>Opens a web address in the user's browser; kept behind an interface so view models stay free of UI types.</summary>
internal interface IUriLauncher
{
    Task LaunchAsync(Uri uri);
}
