using System.IO.Compression;
using System.Text;
using Dotnetstore.PersonalApp.Host.Backup;
using Dotnetstore.PersonalApp.Host.ViewModels;
using Dotnetstore.PersonalApp.Shared.Files;
using Dotnetstore.PersonalApp.Shared.Localization;
using Microsoft.Data.Sqlite;

namespace Dotnetstore.PersonalApp.Integration.Tests;

public sealed class DatabaseBackupTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "personalapp-backup-" + Guid.NewGuid().ToString("N"));
    private readonly DatabaseBackup _backup;

    public DatabaseBackupTests()
    {
        Directory.CreateDirectory(_directory);
        _backup = new DatabaseBackup(_directory, TimeProvider.System);
    }

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();
        Directory.Delete(_directory, recursive: true);
    }

    private void Write(string database, string value)
    {
        using var connection = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = Path.Combine(_directory, database), Pooling = false }.ToString());
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = "CREATE TABLE IF NOT EXISTS Notes (Text TEXT); DELETE FROM Notes; INSERT INTO Notes VALUES ($value)";
        command.Parameters.AddWithValue("$value", value);
        command.ExecuteNonQuery();
    }

    private string Read(string database)
    {
        using var connection = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = Path.Combine(_directory, database), Pooling = false }.ToString());
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT Text FROM Notes";
        return (string)command.ExecuteScalar()!;
    }

    private static byte[] Zip(params (string Name, byte[] Content)[] entries)
    {
        using var stream = new MemoryStream();
        using (var zip = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true))
        {
            foreach (var (name, content) in entries)
            {
                using var entry = zip.CreateEntry(name).Open();
                entry.Write(content);
            }
        }

        return stream.ToArray();
    }

    private static (string, byte[]) Manifest(string version = "1.0.0") =>
        (DatabaseBackup.ManifestName, Encoding.UTF8.GetBytes($$"""{"Format":1,"AppVersion":"{{version}}","CreatedAt":"2026-01-01T00:00:00+00:00"}"""));

    [Fact]
    public async Task BacksUpAndRestoresEveryDatabase()
    {
        var ct = TestContext.Current.CancellationToken;
        Write(DatabaseFiles.Clients, "acme");
        Write(DatabaseFiles.Users, "hasse");

        var made = await _backup.CreateAsync(ct);

        made.IsSuccess.ShouldBeTrue();
        made.Value.DatabaseCount.ShouldBe(2);
        made.Value.FileName.ShouldEndWith(".zip");

        Write(DatabaseFiles.Clients, "changed");
        File.Delete(Path.Combine(_directory, DatabaseFiles.Users));

        var restored = await _backup.RestoreAsync(made.Value.Content, ct);

        restored.IsSuccess.ShouldBeTrue();
        restored.Value.DatabaseCount.ShouldBe(2);
        Read(DatabaseFiles.Clients).ShouldBe("acme");
        Read(DatabaseFiles.Users).ShouldBe("hasse");
    }

    [Fact]
    public async Task RestoreSavesTheReplacedDataFirst()
    {
        var ct = TestContext.Current.CancellationToken;
        Write(DatabaseFiles.Clients, "old");
        var made = await _backup.CreateAsync(ct);
        Write(DatabaseFiles.Clients, "newer");

        var restored = await _backup.RestoreAsync(made.Value.Content, ct);

        restored.Value.SafetyCopyPath.ShouldNotBeNull();
        File.Exists(restored.Value.SafetyCopyPath).ShouldBeTrue();
        using var zip = await ZipFile.OpenReadAsync(restored.Value.SafetyCopyPath, ct);
        zip.GetEntry(DatabaseFiles.Clients).ShouldNotBeNull();
    }

    [Fact]
    public async Task ReportsWhenThereIsNothingToBackUp()
    {
        var result = await _backup.CreateAsync(TestContext.Current.CancellationToken);

        result.Error.Code.ShouldBe("backup_nothing_to_back_up");
    }

    [Fact]
    public async Task RejectsFilesThatAreNotBackupsAndChangesNothing()
    {
        var ct = TestContext.Current.CancellationToken;
        Write(DatabaseFiles.Clients, "keep");
        var good = (await _backup.CreateAsync(ct)).Value;
        var header = (await File.ReadAllBytesAsync(Path.Combine(_directory, DatabaseFiles.Clients), ct)).AsSpan(0, 16).ToArray();

        (await _backup.RestoreAsync(new byte[] { 1, 2, 3 }, ct)).Error.Code.ShouldBe("backup_invalid_archive");
        (await _backup.RestoreAsync(Zip(("clients.db", header)), ct)).Error.Code.ShouldBe("backup_invalid_archive");
        (await _backup.RestoreAsync(Zip(Manifest(), ("notes.txt", [1])), ct)).Error.Code.ShouldBe("backup_invalid_archive");
        (await _backup.RestoreAsync(Zip(Manifest(), ("..\\clients.db", header)), ct)).Error.Code.ShouldBe("backup_invalid_archive");
        (await _backup.RestoreAsync(Zip(Manifest()), ct)).Error.Code.ShouldBe("backup_invalid_archive");
        (await _backup.RestoreAsync(Zip(Manifest("999.0.0"), ("clients.db", good.Content)), ct)).Error.Code.ShouldBe("backup_newer_version");
        (await _backup.RestoreAsync(Zip(Manifest(), ("clients.db", Encoding.UTF8.GetBytes("definitely not sqlite"))), ct)).Error.Code.ShouldBe("backup_corrupt");

        Read(DatabaseFiles.Clients).ShouldBe("keep");
        Directory.Exists(Path.Combine(_directory, DatabaseBackup.SafetyFolder)).ShouldBeFalse();
    }

    [Fact]
    public async Task ViewModelBacksUpThroughTheSaveDialogAndAsksBeforeRestoring()
    {
        Write(DatabaseFiles.Clients, "acme");
        var saver = new FakeSaver();
        var picker = new FakePicker();
        var restarter = new FakeRestarter();
        var viewModel = new BackupViewModel(_backup, saver, picker, restarter, new Localizer());

        await viewModel.BackupCommand.ExecuteAsync(null);
        saver.Content.ShouldNotBeNull();
        viewModel.StatusIsError.ShouldBeFalse();

        Write(DatabaseFiles.Clients, "changed");
        picker.File = new PickedFile("backup.zip", saver.Content);
        await viewModel.ChooseRestoreCommand.ExecuteAsync(null);

        viewModel.IsConfirmingRestore.ShouldBeTrue();
        Read(DatabaseFiles.Clients).ShouldBe("changed");

        await viewModel.ConfirmRestoreCommand.ExecuteAsync(null);

        Read(DatabaseFiles.Clients).ShouldBe("acme");
        viewModel.NeedsRestart.ShouldBeTrue();
        viewModel.BackupCommand.CanExecute(null).ShouldBeFalse();

        viewModel.RestartCommand.Execute(null);
        restarter.Restarted.ShouldBeTrue();
    }

    private sealed class FakeSaver : IFileSaveService
    {
        public byte[]? Content { get; private set; }

        public Task<string?> SaveAsync(string title, string suggestedFileName, byte[] content, CancellationToken cancellationToken)
        {
            Content = content;
            return Task.FromResult<string?>(suggestedFileName);
        }
    }

    private sealed class FakePicker : IFilePickService
    {
        public PickedFile? File { get; set; }

        public Task<FilePickResult> PickFilesAsync(string title, long maxBytes, CancellationToken cancellationToken) =>
            Task.FromResult(new FilePickResult(File is null ? [] : [File], []));
    }

    private sealed class FakeRestarter : IAppRestarter
    {
        public bool Restarted { get; private set; }

        public void Restart() => Restarted = true;
    }
}
