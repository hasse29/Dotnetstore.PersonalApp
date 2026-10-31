namespace Dotnetstore.PersonalApp.Shared.Startup;

/// <summary>
/// Work a module (or the host) needs to finish before the main window opens, e.g. applying migrations or warming caches.
/// Each registered task is run in order and shown as a step on the splash screen.
/// </summary>
public interface IStartupTask
{
    string Description { get; }

    Task RunAsync(CancellationToken cancellationToken);
}
