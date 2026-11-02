using Dotnetstore.PersonalApp.Modules.Subjects.Features.Subjects;
using Dotnetstore.PersonalApp.Modules.Subjects.Features.TeacherSubjects;
using Dotnetstore.PersonalApp.Modules.Subjects.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Dotnetstore.PersonalApp.Modules.Subjects.Tests;

/// <summary>
/// Real SQLite (in-memory, kept alive by one open connection) with the module's handlers wired up.
/// </summary>
internal sealed class SubjectsFixture : IDisposable
{
    private readonly SqliteConnection _connection = new("Data Source=:memory:");

    public SubjectsFixture()
    {
        _connection.Open();
        DbFactory = new TestDbFactory(new DbContextOptionsBuilder<SubjectsDbContext>().UseSqlite(_connection).Options);
        using var db = DbFactory.CreateDbContext();
        db.Database.EnsureCreated();

        Create = new CreateSubject.Handler(DbFactory, new CreateSubject.Validator(), Clock);
        Update = new UpdateSubject.Handler(DbFactory, new UpdateSubject.Validator());
        List = new ListSubjects.Handler(DbFactory);
        Delete = new DeleteSubject.Handler(DbFactory);
        ListTaught = new ListTeacherSubjects.Handler(DbFactory);
        AddTaught = new AddTeacherSubject.Handler(DbFactory, Clock);
        RemoveTaught = new RemoveTeacherSubject.Handler(DbFactory);
        DeleteForUser = new DeleteTeacherSubjectsForUser.Handler(DbFactory);
    }

    public TimeProvider Clock { get; } = new FixedClock(new DateTimeOffset(2026, 11, 2, 12, 0, 0, TimeSpan.Zero));

    public IDbContextFactory<SubjectsDbContext> DbFactory { get; }

    public CreateSubject.Handler Create { get; }

    public UpdateSubject.Handler Update { get; }

    public ListSubjects.Handler List { get; }

    public DeleteSubject.Handler Delete { get; }

    public ListTeacherSubjects.Handler ListTaught { get; }

    public AddTeacherSubject.Handler AddTaught { get; }

    public RemoveTeacherSubject.Handler RemoveTaught { get; }

    public DeleteTeacherSubjectsForUser.Handler DeleteForUser { get; }

    public async Task<SubjectDto> CreateSubjectAsync(string name, CancellationToken cancellationToken) =>
        (await Create.HandleAsync(new CreateSubject.Command(name, null), cancellationToken)).Value;

    public void Dispose() => _connection.Dispose();

    private sealed class TestDbFactory(DbContextOptions<SubjectsDbContext> options) : IDbContextFactory<SubjectsDbContext>
    {
        public SubjectsDbContext CreateDbContext() => new(options);
    }

    private sealed class FixedClock(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
