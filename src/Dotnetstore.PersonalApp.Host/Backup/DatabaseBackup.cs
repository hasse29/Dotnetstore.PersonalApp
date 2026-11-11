using System.IO.Compression;
using System.Text.Json;
using Dotnetstore.PersonalApp.Host.Updates;
using Dotnetstore.PersonalApp.Shared.Results;
using Microsoft.Data.Sqlite;

namespace Dotnetstore.PersonalApp.Host.Backup;

internal sealed record BackupFile(string FileName, byte[] Content, int DatabaseCount);

/// <param name="SafetyCopyPath">The zip of the data that was replaced, or null when there was nothing to replace.</param>
internal sealed record RestoreSummary(int DatabaseCount, string? SafetyCopyPath);

/// <summary>
/// Packs every module database in the data directory into one zip and puts such a zip back. Databases are copied with SQLite's
/// online backup, so the copy is consistent even while the app is using them. A restore checks the whole archive before it
/// changes anything and first saves the current data to <c>Backups/</c>, so a wrong file can be undone.
/// </summary>
internal sealed class DatabaseBackup(string dataDirectory, TimeProvider clock)
{
    public const long MaxBackupBytes = 1024L * 1024 * 1024;
    public const string ManifestName = "backup.json";
    public const string SafetyFolder = "Backups";

    private const int ManifestFormat = 1;
    private static readonly byte[] SqliteHeader = "SQLite format 3\0"u8.ToArray();

    private sealed record Manifest(int Format, string AppVersion, DateTimeOffset CreatedAt);

    public async Task<Result<BackupFile>> CreateAsync(CancellationToken cancellationToken)
    {
        var staging = NewStagingDirectory(Path.GetTempPath());
        try
        {
            return await Task.Run(() => Create(staging), cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or SqliteException)
        {
            return Error.Validation("backup_failed", "The backup could not be created.");
        }
        finally
        {
            DeleteQuietly(staging);
        }
    }

    public async Task<Result<RestoreSummary>> RestoreAsync(ReadOnlyMemory<byte> archive, CancellationToken cancellationToken)
    {
        // Staged inside the data directory so the final move onto the live files stays on one volume.
        var staging = NewStagingDirectory(dataDirectory);
        try
        {
            return await Task.Run(() => Restore(archive, staging), cancellationToken).ConfigureAwait(false);
        }
        catch (InvalidDataException)
        {
            return InvalidArchive();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or SqliteException)
        {
            return Error.Validation("backup_restore_failed", "The databases could not be restored.");
        }
        finally
        {
            DeleteQuietly(staging);
        }
    }

    private Result<BackupFile> Create(string staging)
    {
        var copies = new List<(string Name, string Path)>();
        foreach (var name in DatabaseFiles.All)
        {
            var source = Path.Combine(dataDirectory, name);
            if (!File.Exists(source))
            {
                continue;
            }

            var copy = Path.Combine(staging, name);
            using (var from = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = source, Mode = SqliteOpenMode.ReadOnly, Pooling = false }.ToString()))
            using (var to = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = copy, Pooling = false }.ToString()))
            {
                from.Open();
                to.Open();
                from.BackupDatabase(to);
            }

            copies.Add((name, copy));
        }

        if (copies.Count == 0)
        {
            return Error.NotFound("backup_nothing_to_back_up", "There are no databases to back up.");
        }

        using var stream = new MemoryStream();
        using (var zip = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true))
        {
            var manifest = new Manifest(ManifestFormat, GitHubUpdateChecker.Current.ToString(), clock.GetUtcNow());
            using (var writer = zip.CreateEntry(ManifestName).Open())
            {
                JsonSerializer.Serialize(writer, manifest);
            }

            foreach (var (name, path) in copies)
            {
                zip.CreateEntryFromFile(path, name, CompressionLevel.Optimal);
            }
        }

        var fileName = string.Create(System.Globalization.CultureInfo.InvariantCulture, $"Dotnetstore.PersonalApp-backup-{clock.GetLocalNow():yyyyMMdd-HHmm}.zip");
        return new BackupFile(fileName, stream.ToArray(), copies.Count);
    }

    private Result<RestoreSummary> Restore(ReadOnlyMemory<byte> archive, string staging)
    {
        // A file that is not a zip throws InvalidDataException here, which RestoreAsync reports as an invalid archive.
        using var zip = new ZipArchive(new MemoryStream(archive.ToArray()), ZipArchiveMode.Read);
        var staged = Stage(zip, staging);
        if (!staged.IsSuccess)
        {
            return staged.Error;
        }

        string? safetyCopy = null;
        var current = Create(NewStagingDirectory(staging));
        if (current.IsSuccess)
        {
            var folder = Path.Combine(dataDirectory, SafetyFolder);
            Directory.CreateDirectory(folder);
            safetyCopy = Path.Combine(folder, current.Value.FileName.Replace("-backup-", "-before-restore-", StringComparison.Ordinal));
            File.WriteAllBytes(safetyCopy, current.Value.Content);
        }

        // Pooled connections would keep the old files open (and in use) on Windows.
        SqliteConnection.ClearAllPools();
        foreach (var (name, path) in staged.Value)
        {
            foreach (var sidecar in new[] { "-wal", "-shm", "-journal" })
            {
                File.Delete(Path.Combine(dataDirectory, name + sidecar));
            }

            File.Move(path, Path.Combine(dataDirectory, name), overwrite: true);
        }

        return new RestoreSummary(staged.Value.Count, safetyCopy);
    }

    /// <summary>Extracts and checks every database in the archive; nothing in the data directory is touched.</summary>
    private static Result<List<(string Name, string Path)>> Stage(ZipArchive zip, string staging)
    {
        var manifestEntry = zip.GetEntry(ManifestName);
        if (manifestEntry is null || ReadManifest(manifestEntry) is not { Format: ManifestFormat } manifest)
        {
            return InvalidArchive();
        }

        if (GitHubUpdateChecker.ParseVersion(manifest.AppVersion) is { } version && version > GitHubUpdateChecker.Current)
        {
            return Error.Validation("backup_newer_version", "The backup was made by a newer version of the app.");
        }

        var staged = new List<(string Name, string Path)>();
        foreach (var entry in zip.Entries.Where(e => e.FullName != ManifestName))
        {
            // A backup from before a module was removed still has its database; it is simply not restored.
            if (DatabaseFiles.Retired.Contains(entry.FullName, StringComparer.Ordinal))
            {
                continue;
            }

            var name = DatabaseFiles.All.FirstOrDefault(n => string.Equals(n, entry.FullName, StringComparison.Ordinal));
            if (name is null || staged.Any(s => s.Name == name))
            {
                return InvalidArchive();
            }

            if (entry.Length > MaxBackupBytes)
            {
                return Error.Validation("backup_file_too_large", "The backup is too large.");
            }

            var path = Path.Combine(staging, name);
            entry.ExtractToFile(path);
            if (!LooksHealthy(path))
            {
                return Error.Validation("backup_corrupt", "The backup contains a damaged database.");
            }

            staged.Add((name, path));
        }

        return staged.Count == 0 ? InvalidArchive() : staged;
    }

    private static Manifest? ReadManifest(ZipArchiveEntry entry)
    {
        try
        {
            using var stream = entry.Open();
            return JsonSerializer.Deserialize<Manifest>(stream);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static bool LooksHealthy(string path)
    {
        var header = new byte[SqliteHeader.Length];
        using (var file = File.OpenRead(path))
        {
            if (file.Read(header) != header.Length || !header.AsSpan().SequenceEqual(SqliteHeader))
            {
                return false;
            }
        }

        try
        {
            using var connection = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = path, Mode = SqliteOpenMode.ReadOnly, Pooling = false }.ToString());
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText = "PRAGMA quick_check";
            return string.Equals(command.ExecuteScalar() as string, "ok", StringComparison.Ordinal);
        }
        catch (SqliteException)
        {
            return false;
        }
    }

    private static Error InvalidArchive() => Error.Validation("backup_invalid_archive", "This is not a backup made by this app.");

    private static string NewStagingDirectory(string parent)
    {
        var path = Path.Combine(parent, ".backup-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }

    private static void DeleteQuietly(string directory)
    {
        try
        {
            Directory.Delete(directory, recursive: true);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // Leftover staging files are harmless and are never read back.
        }
    }
}
