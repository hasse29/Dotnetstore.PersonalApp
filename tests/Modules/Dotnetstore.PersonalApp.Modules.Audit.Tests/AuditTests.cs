using System.Collections;
using System.Globalization;
using Dotnetstore.PersonalApp.Modules.Audit.Features.Entries;
using Dotnetstore.PersonalApp.Modules.Audit.Persistence;
using Dotnetstore.PersonalApp.Modules.Audit.Resources;
using Dotnetstore.PersonalApp.Shared.Audit;
using Dotnetstore.PersonalApp.Shared.Auth;
using Dotnetstore.PersonalApp.Shared.Results;
using Dotnetstore.PersonalApp.Shared.Telemetry;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace AuditFakes
{
    // Outside a ".Modules." namespace on purpose: handlers of the Audit module itself are never audited.
    internal static class SaveThing
    {
        internal sealed record Command(Guid Id, string Name, string? Notes, string? Empty, Guid OwnerId);

        internal sealed class Handler;
    }

    internal static class FindThings
    {
        internal sealed record Query(string Search);

        internal sealed class Handler;
    }

    internal sealed record Thing(Guid Id);
}

namespace Dotnetstore.PersonalApp.Modules.Audit.Tests
{
    // AuditTrail is process wide, so everything that touches it runs in this one class.
    public sealed class AuditTrailTests : IDisposable
    {
        private readonly RecordingSink _sink = new();
        private readonly AuthenticatedUser _user = new(Guid.NewGuid(), "anna", "Anna Andersson");

        public AuditTrailTests()
        {
            AuditTrail.Sink = _sink;
            AuditTrail.CurrentUser = () => _user;
        }

        public void Dispose()
        {
            AuditTrail.Sink = null;
            AuditTrail.CurrentUser = static () => null;
        }

        [Fact]
        public async Task ACommandIsRecordedAsAChangeWithTheEntityIdAndOnlyTheNamesOfTheFilledInFields()
        {
            var id = Guid.NewGuid();
            var command = new AuditFakes.SaveThing.Command(id, "Secret name", "Secret notes", string.Empty, Guid.NewGuid());

            await AppTelemetry.TraceAsync(new AuditFakes.SaveThing.Handler(), () => Task.FromResult(Result<AuditFakes.Thing>.Success(new AuditFakes.Thing(id))), command);

            var entry = _sink.Entries.Single();
            entry.Operation.ShouldBe("SaveThing");
            entry.Kind.ShouldBe(AuditKind.Write);
            entry.EntityId.ShouldBe(id);
            entry.Succeeded.ShouldBeTrue();
            entry.UserId.ShouldBe(_user.Id);
            entry.Username.ShouldBe("anna");
            entry.Fields.ShouldBe(["Name", "Notes"]);
            string.Join(' ', entry.Fields).ShouldNotContain("Secret");
        }

        [Fact]
        public async Task AQueryIsRecordedAsARead()
        {
            await AppTelemetry.TraceAsync(new AuditFakes.FindThings.Handler(), () => Task.FromResult(1), new AuditFakes.FindThings.Query("secret"));

            var entry = _sink.Entries.Single();
            entry.Kind.ShouldBe(AuditKind.Read);
            entry.Fields.ShouldBeEmpty();
        }

        [Fact]
        public async Task AFailedResultIsRecordedWithItsErrorCode()
        {
            var command = new AuditFakes.SaveThing.Command(Guid.NewGuid(), "x", null, null, Guid.NewGuid());

            await AppTelemetry.TraceAsync(
                new AuditFakes.SaveThing.Handler(),
                () => Task.FromResult(Result<AuditFakes.Thing>.Failure(Error.Validation("thing_invalid", "text"))),
                command);

            var entry = _sink.Entries.Single();
            entry.Succeeded.ShouldBeFalse();
            entry.ErrorCode.ShouldBe("thing_invalid");
        }

        [Fact]
        public async Task AnExceptionIsRecordedAsAFailureAndStillThrown()
        {
            await Should.ThrowAsync<InvalidOperationException>(() =>
                AppTelemetry.TraceAsync<int>(new AuditFakes.FindThings.Handler(), () => throw new InvalidOperationException("boom"), new AuditFakes.FindThings.Query("q")));

            var entry = _sink.Entries.Single();
            entry.Succeeded.ShouldBeFalse();
            entry.ErrorCode.ShouldBe("exception");
        }

        [Fact]
        public async Task NothingIsRecordedWhenNoSinkIsConnected()
        {
            AuditTrail.Sink = null;

            await AppTelemetry.TraceAsync(new AuditFakes.FindThings.Handler(), () => Task.FromResult(1), new AuditFakes.FindThings.Query("q"));

            _sink.Entries.ShouldBeEmpty();
        }

        [Fact]
        public async Task ReadingTheAuditLogIsNotItselfAudited()
        {
            await using var fixture = new AuditFixture();

            await new ListAuditEntries.Handler(fixture.DbFactory).HandleAsync(new ListAuditEntries.Query(), TestContext.Current.CancellationToken);

            _sink.Entries.ShouldBeEmpty();
        }
    }

    public sealed class AuditWriterTests
    {
        private static CancellationToken Ct => TestContext.Current.CancellationToken;

        [Fact]
        public async Task EntriesAreWrittenInTheBackgroundAndListedNewestFirstWithoutValues()
        {
            await using var fixture = new AuditFixture();
            var writer = new AuditWriter(fixture.DbFactory);
            var user = Guid.NewGuid();
            var start = new DateTimeOffset(2026, 11, 12, 10, 0, 0, TimeSpan.Zero);

            // Recorded before Start: queued while the database is still being prepared.
            writer.Record(new AuditEntry(start, user, "anna", "Clients.CreateClient", AuditKind.Write, Guid.NewGuid(), true, null, ["Name", "Email"], 4));
            writer.Start();
            writer.Record(new AuditEntry(start.AddMinutes(1), user, "anna", "Clients.DeleteClient", AuditKind.Write, Guid.NewGuid(), false, "client_not_found", [], 2));
            writer.Record(new AuditEntry(start.AddMinutes(2), user, "anna", "Clients.ListClients", AuditKind.Read, null, true, null, [], 1));
            await writer.DisposeAsync();

            var list = new ListAuditEntries.Handler(fixture.DbFactory);
            var all = await list.HandleAsync(new ListAuditEntries.Query(), Ct);
            all.TotalCount.ShouldBe(3);
            all.Items.Select(i => i.Operation).ShouldBe(["Clients.ListClients", "Clients.DeleteClient", "Clients.CreateClient"]);
            all.Items[^1].Fields.ShouldBe("Name, Email");

            (await list.HandleAsync(new ListAuditEntries.Query(Writes: true), Ct)).TotalCount.ShouldBe(2);
            (await list.HandleAsync(new ListAuditEntries.Query(OnlyFailed: true), Ct)).Items.Single().ErrorCode.ShouldBe("client_not_found");
            (await list.HandleAsync(new ListAuditEntries.Query("CreateClient"), Ct)).Items.Single().Outcome.ShouldBe("OK");
        }
    }

    public sealed class AuditSchemaAndTextTests
    {
        private static HashSet<string> Keys(string culture)
        {
            var set = AuditStrings.ResourceManager.GetResourceSet(
                culture == "en" ? CultureInfo.InvariantCulture : CultureInfo.GetCultureInfo(culture),
                createIfNotExists: true,
                tryParents: false);
            return set is null ? [] : [.. set.Cast<DictionaryEntry>().Select(e => (string)e.Key)];
        }

        [Fact]
        public async Task MigrationsCreateTheSchemaMatchingTheModel()
        {
            await using var connection = new SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync(TestContext.Current.CancellationToken);
            await using var db = new AuditDbContext(new DbContextOptionsBuilder<AuditDbContext>().UseSqlite(connection).Options);

            await db.Database.MigrateAsync(TestContext.Current.CancellationToken);

            (await db.Records.ToListAsync(TestContext.Current.CancellationToken)).ShouldBeEmpty();
            db.Database.HasPendingModelChanges().ShouldBeFalse();
        }

        [Theory]
        [InlineData("sv")]
        [InlineData("zh-Hans")]
        public void EveryEnglishKeyIsTranslated(string culture)
        {
            var english = Keys("en");

            english.ShouldNotBeEmpty();
            english.Except(Keys(culture)).ShouldBeEmpty();
            Keys(culture).Except(english).ShouldBeEmpty();
        }
    }

    internal sealed class RecordingSink : IAuditSink
    {
        public List<AuditEntry> Entries { get; } = [];

        public void Record(AuditEntry entry) => Entries.Add(entry);
    }

    /// <summary>Real SQLite (in-memory, kept alive by one open connection) with the schema created.</summary>
    internal sealed class AuditFixture : IAsyncDisposable
    {
        private readonly SqliteConnection _connection = new("Data Source=:memory:");

        public AuditFixture()
        {
            _connection.Open();
            DbFactory = new Factory(new DbContextOptionsBuilder<AuditDbContext>().UseSqlite(_connection).Options);
            using var db = DbFactory.CreateDbContext();
            db.Database.EnsureCreated();
        }

        public IDbContextFactory<AuditDbContext> DbFactory { get; }

        public ValueTask DisposeAsync() => _connection.DisposeAsync();

        private sealed class Factory(DbContextOptions<AuditDbContext> options) : IDbContextFactory<AuditDbContext>
        {
            public AuditDbContext CreateDbContext() => new(options);
        }
    }
}
