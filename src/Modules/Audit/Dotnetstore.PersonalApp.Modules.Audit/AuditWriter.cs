using System.Threading.Channels;
using Dotnetstore.PersonalApp.Modules.Audit.Domain;
using Dotnetstore.PersonalApp.Modules.Audit.Persistence;
using Dotnetstore.PersonalApp.Shared.Audit;
using Dotnetstore.PersonalApp.Shared.Telemetry;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Dotnetstore.PersonalApp.Modules.Audit;

/// <summary>
/// Receives audit entries from every handler and writes them to audit.db on a background loop, so an operation never waits for
/// the audit log and a failure to write it never fails the operation. Entries recorded before <see cref="Start"/> (the database
/// is still being prepared at startup) are queued and written once it is called.
/// </summary>
internal sealed class AuditWriter(IDbContextFactory<AuditDbContext> dbFactory) : IAuditSink, IAsyncDisposable, IDisposable
{
    private const int BatchSize = 100;

    private readonly Channel<AuditEntry> _queue = Channel.CreateUnbounded<AuditEntry>(new UnboundedChannelOptions { SingleReader = true });
    private readonly Lock _gate = new();
    private Task? _loop;

    public void Record(AuditEntry entry) => _queue.Writer.TryWrite(entry);

    public void Start()
    {
        lock (_gate)
        {
            _loop ??= Task.Run(RunAsync);
        }
    }

    // The service provider disposes singletons synchronously, so the flush on exit goes through here.
    public void Dispose()
    {
        _queue.Writer.TryComplete();
        Task? loop;
        lock (_gate)
        {
            loop = _loop;
        }

        loop?.Wait(TimeSpan.FromSeconds(5));
    }

    public async ValueTask DisposeAsync()
    {
        _queue.Writer.TryComplete();
        Task? loop;
        lock (_gate)
        {
            loop = _loop;
        }

        if (loop is not null)
        {
            // The last entries (a restore, a sign-out) must reach the file, but shutting down must not hang on a stuck disk.
            try
            {
                await loop.WaitAsync(TimeSpan.FromSeconds(5)).ConfigureAwait(false);
            }
            catch (TimeoutException)
            {
                // Give up on the remaining entries rather than block the shutdown.
            }
        }
    }

    private async Task RunAsync()
    {
        var batch = new List<AuditEntry>(BatchSize);
        while (await _queue.Reader.WaitToReadAsync().ConfigureAwait(false))
        {
            while (batch.Count < BatchSize && _queue.Reader.TryRead(out var entry))
            {
                batch.Add(entry);
            }

            await WriteAsync(batch).ConfigureAwait(false);
            batch.Clear();
        }
    }

    private async Task WriteAsync(List<AuditEntry> batch)
    {
        try
        {
            await using var db = await dbFactory.CreateDbContextAsync().ConfigureAwait(false);
            db.Records.AddRange(batch.Select(ToRecord));
            await db.SaveChangesAsync().ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is DbUpdateException or SqliteException or IOException or InvalidOperationException)
        {
            // Losing audit lines is serious but must not take the app down; the application log says it happened.
            AppTelemetry.CreateLogger<AuditWriter>().LogHandled(ex, "Audit.Write");
        }
    }

    private static AuditRecord ToRecord(AuditEntry entry) => new()
    {
        Id = Guid.CreateVersion7(entry.Timestamp),
        Timestamp = entry.Timestamp,
        UserId = entry.UserId,
        Username = entry.Username,
        Operation = entry.Operation,
        IsWrite = entry.Kind == AuditKind.Write,
        EntityId = entry.EntityId,
        Succeeded = entry.Succeeded,
        ErrorCode = entry.ErrorCode,
        Fields = entry.Fields.Count == 0 ? null : string.Join(", ", entry.Fields),
        ElapsedMs = entry.ElapsedMs,
    };
}
