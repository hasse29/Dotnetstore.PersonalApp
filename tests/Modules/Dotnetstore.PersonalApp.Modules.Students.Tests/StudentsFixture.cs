using Dotnetstore.PersonalApp.Modules.Students.Features.Students;
using Dotnetstore.PersonalApp.Modules.Students.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Dotnetstore.PersonalApp.Modules.Students.Tests;

/// <summary>
/// Real SQLite (in-memory, kept alive by one open connection) with the module's handlers wired up.
/// </summary>
internal sealed class StudentsFixture : IDisposable
{
    private readonly SqliteConnection _connection = new("Data Source=:memory:");

    public StudentsFixture()
    {
        _connection.Open();
        DbFactory = new TestDbFactory(new DbContextOptionsBuilder<StudentsDbContext>().UseSqlite(_connection).Options);
        using var db = DbFactory.CreateDbContext();
        db.Database.EnsureCreated();

        Create = new CreateStudent.Handler(DbFactory, new CreateStudent.Validator(), Clock);
        Update = new UpdateStudent.Handler(DbFactory, new UpdateStudent.Validator());
        List = new ListStudents.Handler(DbFactory);
        Delete = new DeleteStudent.Handler(DbFactory, Events);
        DeleteForClient = new DeleteStudentsForClient.Handler(DbFactory, Events);
    }

    public TimeProvider Clock { get; } = new FixedClock(new DateTimeOffset(2026, 11, 2, 12, 0, 0, TimeSpan.Zero));

    public IDbContextFactory<StudentsDbContext> DbFactory { get; }

    public CreateStudent.Handler Create { get; }

    public UpdateStudent.Handler Update { get; }

    public ListStudents.Handler List { get; }

    public DeleteStudent.Handler Delete { get; }

    public DeleteStudentsForClient.Handler DeleteForClient { get; }

    public RecordingEventPublisher Events { get; } = new();

    public void Dispose() => _connection.Dispose();

    private sealed class TestDbFactory(DbContextOptions<StudentsDbContext> options) : IDbContextFactory<StudentsDbContext>
    {
        public StudentsDbContext CreateDbContext() => new(options);
    }

    private sealed class FixedClock(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
