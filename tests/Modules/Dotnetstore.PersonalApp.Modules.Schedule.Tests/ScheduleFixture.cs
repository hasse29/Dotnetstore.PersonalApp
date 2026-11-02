using System.Globalization;
using Dotnetstore.PersonalApp.Modules.Schedule.Features.Entries;
using Dotnetstore.PersonalApp.Modules.Schedule.Persistence;
using Dotnetstore.PersonalApp.Shared.Clients;
using Dotnetstore.PersonalApp.Shared.Students;
using Dotnetstore.PersonalApp.Shared.Subjects;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Dotnetstore.PersonalApp.Modules.Schedule.Tests;

/// <summary>
/// Real SQLite (in-memory, kept alive by one open connection) with the module's handlers wired up.
/// Subjects and schools belong to other modules, so they are faked behind the shared lookups.
/// </summary>
internal sealed class ScheduleFixture : IDisposable
{
    private readonly SqliteConnection _connection = new("Data Source=:memory:");

    public ScheduleFixture()
    {
        _connection.Open();
        DbFactory = new TestDbFactory(new DbContextOptionsBuilder<ScheduleDbContext>().UseSqlite(_connection).Options);
        using var db = DbFactory.CreateDbContext();
        db.Database.EnsureCreated();

        Create = new CreateScheduleEntry.Handler(DbFactory, new CreateScheduleEntry.Validator(), Subjects, Schools, Students, Clock);
        Update = new UpdateScheduleEntry.Handler(DbFactory, new UpdateScheduleEntry.Validator(), Subjects, Schools, Students);
        Delete = new DeleteScheduleEntry.Handler(DbFactory);
        List = new ListScheduleEntries.Handler(DbFactory);
        DeleteFor = new DeleteScheduleEntriesFor.Handler(DbFactory);
        RemoveStudent = new RemoveStudentFromEntries.Handler(DbFactory);
    }

    public TimeProvider Clock { get; } = new FixedClock(new DateTimeOffset(2026, 11, 2, 12, 0, 0, TimeSpan.Zero));

    public IDbContextFactory<ScheduleDbContext> DbFactory { get; }

    public FakeSubjects Subjects { get; } = new();

    public FakeSchools Schools { get; } = new();

    public FakeStudents Students { get; } = new();

    public CreateScheduleEntry.Handler Create { get; }

    public UpdateScheduleEntry.Handler Update { get; }

    public DeleteScheduleEntry.Handler Delete { get; }

    public ListScheduleEntries.Handler List { get; }

    public DeleteScheduleEntriesFor.Handler DeleteFor { get; }

    public RemoveStudentFromEntries.Handler RemoveStudent { get; }

    public Task<Dotnetstore.PersonalApp.Shared.Results.Result<ScheduleEntryDto>> CreateAsync(
        Guid userId,
        DayOfWeek day,
        string start,
        string end,
        SubjectReference subject,
        ClientReference school,
        CancellationToken cancellationToken,
        IReadOnlyList<Guid>? studentIds = null) =>
        Create.HandleAsync(
            new CreateScheduleEntry.Command(userId, day, TimeOnly.Parse(start, CultureInfo.InvariantCulture), TimeOnly.Parse(end, CultureInfo.InvariantCulture), subject.Id, school.Id, null, null, studentIds ?? []),
            cancellationToken);

    public void Dispose() => _connection.Dispose();

    internal sealed class FakeSubjects : ISubjectLookup
    {
        private readonly List<SubjectReference> _items = [];

        public SubjectReference Add(string name)
        {
            var subject = new SubjectReference(Guid.NewGuid(), name);
            _items.Add(subject);
            return subject;
        }

        public Task<IReadOnlyList<SubjectReference>> ListAsync(CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<SubjectReference>>([.. _items.OrderBy(s => s.Name)]);

        public Task<bool> ExistsAsync(Guid subjectId, CancellationToken cancellationToken) =>
            Task.FromResult(_items.Any(s => s.Id == subjectId));
    }

    internal sealed class FakeSchools : IClientLookup
    {
        private readonly List<ClientReference> _items = [];

        public ClientReference Add(string name)
        {
            var school = new ClientReference(Guid.NewGuid(), name);
            _items.Add(school);
            return school;
        }

        public Task<IReadOnlyList<ClientReference>> ListAsync(CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<ClientReference>>([.. _items.OrderBy(c => c.Name)]);
    }

    internal sealed class FakeStudents : IStudentLookup
    {
        private readonly List<StudentReference> _items = [];

        public StudentReference Add(ClientReference school, string name, string? className = null)
        {
            var student = new StudentReference(Guid.NewGuid(), school.Id, name, className);
            _items.Add(student);
            return student;
        }

        public Task<IReadOnlyList<StudentReference>> ListForSchoolAsync(Guid clientId, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<StudentReference>>([.. _items.Where(s => s.ClientId == clientId).OrderBy(s => s.Name)]);
    }

    private sealed class TestDbFactory(DbContextOptions<ScheduleDbContext> options) : IDbContextFactory<ScheduleDbContext>
    {
        public ScheduleDbContext CreateDbContext() => new(options);
    }

    private sealed class FixedClock(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
