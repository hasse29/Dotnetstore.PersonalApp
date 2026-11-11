using Dotnetstore.PersonalApp.Host.Backup;
using Dotnetstore.PersonalApp.Shared.Localization;
using Dotnetstore.PersonalApp.Shared.Startup;

namespace Dotnetstore.PersonalApp.Host.Startup;

/// <summary>Deletes the databases of modules that no longer exist (Production, Purchasing, Sales and Inventory), with their journal files.</summary>
internal sealed class RemoveRetiredDatabasesTask(string dataDirectory, ILocalizationService localization) : IStartupTask
{
    private static readonly string[] Suffixes = ["", "-wal", "-shm", "-journal"];

    public string Description => localization.Get("Startup_RemovingRetiredDatabases");

    public Task RunAsync(CancellationToken cancellationToken)
    {
        foreach (var name in DatabaseFiles.Retired)
        {
            foreach (var suffix in Suffixes)
            {
                File.Delete(Path.Combine(dataDirectory, name + suffix));
            }
        }

        return Task.CompletedTask;
    }
}
