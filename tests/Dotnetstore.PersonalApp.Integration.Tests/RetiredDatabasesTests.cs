using System.IO.Compression;
using System.Text;
using Dotnetstore.PersonalApp.Host;
using Dotnetstore.PersonalApp.Host.Backup;
using Dotnetstore.PersonalApp.Host.Startup;
using Dotnetstore.PersonalApp.Shared.Shell;
using Dotnetstore.PersonalApp.Shared.Startup;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;

namespace Dotnetstore.PersonalApp.Integration.Tests;

public sealed class RetiredDatabasesTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "personalapp-retired-" + Guid.NewGuid().ToString("N"));

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, recursive: true);
        }
    }

    [Fact]
    public async Task StartupDeletesTheDatabasesOfRemovedModulesAndKeepsTheRest()
    {
        Directory.CreateDirectory(_directory);
        foreach (var name in DatabaseFiles.Retired)
        {
            await File.WriteAllTextAsync(Path.Combine(_directory, name), "old", Ct);
            await File.WriteAllTextAsync(Path.Combine(_directory, name + "-wal"), "old", Ct);
        }

        await File.WriteAllTextAsync(Path.Combine(_directory, DatabaseFiles.Clients), "keep", Ct);

        using var services = CompositionRoot.BuildServices(_directory);
        foreach (var task in services.GetServices<IStartupTask>().OfType<RemoveRetiredDatabasesTask>())
        {
            await task.RunAsync(Ct);
        }

        Directory.EnumerateFiles(_directory).Select(Path.GetFileName).ShouldBe([DatabaseFiles.Clients]);
    }

    [Fact]
    public async Task RestoreIgnoresARetiredDatabaseInAnOlderBackup()
    {
        Directory.CreateDirectory(_directory);
        await using (var connection = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = Path.Combine(_directory, DatabaseFiles.Clients), Pooling = false }.ToString()))
        {
            await connection.OpenAsync(Ct);
            await using var command = connection.CreateCommand();
            command.CommandText = "CREATE TABLE Notes (Text TEXT)";
            await command.ExecuteNonQueryAsync(Ct);
        }

        var backup = new DatabaseBackup(_directory, TimeProvider.System);
        var made = (await backup.CreateAsync(Ct)).Value;

        using var stream = new MemoryStream();
        await stream.WriteAsync(made.Content, Ct);
        using (var zip = new ZipArchive(stream, ZipArchiveMode.Update, leaveOpen: true))
        {
            await using var old = await zip.CreateEntry("sales.db").OpenAsync(Ct);
            await old.WriteAsync(Encoding.UTF8.GetBytes("not even sqlite"), Ct);
        }

        var restored = await backup.RestoreAsync(stream.ToArray(), Ct);

        restored.IsSuccess.ShouldBeTrue();
        restored.Value.DatabaseCount.ShouldBe(1);
        File.Exists(Path.Combine(_directory, "sales.db")).ShouldBeFalse();
    }

    [Fact]
    public void TheRemovedModulesAreNotRegistered()
    {
        using var services = CompositionRoot.BuildServices(_directory);

        // "sales" is not listed: the module of that name was replaced by a new one that keeps the key (and uses another database file).
        services.GetServices<ModuleInfo>().Select(m => m.Key).Intersect(["production", "purchasing", "inventory"]).ShouldBeEmpty();
    }
}
