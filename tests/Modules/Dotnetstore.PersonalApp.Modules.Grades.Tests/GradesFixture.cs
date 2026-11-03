using Dotnetstore.PersonalApp.Modules.Grades.Features.Grades;
using Dotnetstore.PersonalApp.Modules.Grades.Features.Scales;
using Dotnetstore.PersonalApp.Modules.Grades.Features.Tests;
using Dotnetstore.PersonalApp.Modules.Grades.Persistence;
using Dotnetstore.PersonalApp.Shared.Students;
using Dotnetstore.PersonalApp.Shared.Subjects;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Dotnetstore.PersonalApp.Modules.Grades.Tests;

/// <summary>
/// Real SQLite (in-memory, kept alive by one open connection) with the module's handlers wired up.
/// Students and subjects belong to other modules, so they are faked behind the shared lookups.
/// </summary>
internal sealed class GradesFixture : IDisposable
{
    private readonly SqliteConnection _connection = new("Data Source=:memory:");

    public GradesFixture()
    {
        _connection.Open();
        DbFactory = new TestDbFactory(new DbContextOptionsBuilder<GradesDbContext>().UseSqlite(_connection).Options);
        using var db = DbFactory.CreateDbContext();
        db.Database.EnsureCreated();

        CreateScale = new CreateGradeScale.Handler(DbFactory, new CreateGradeScale.Validator(), Clock);
        UpdateScale = new UpdateGradeScale.Handler(DbFactory, new UpdateGradeScale.Validator());
        DeleteScale = new DeleteGradeScale.Handler(DbFactory);
        ListScales = new ListGradeScales.Handler(DbFactory);
        CreateGrade = new CreateGrade.Handler(DbFactory, new CreateGrade.Validator(), Students, Subjects, Clock);
        UpdateGrade = new UpdateGrade.Handler(DbFactory, new UpdateGrade.Validator(), Students, Subjects);
        DeleteGrade = new DeleteGrade.Handler(DbFactory);
        ListGrades = new ListGrades.Handler(DbFactory);
        DeleteGradesFor = new DeleteGradesFor.Handler(DbFactory);
        CreateTest = new CreateTest.Handler(DbFactory, new CreateTest.Validator(), Clock);
        UpdateTest = new UpdateTest.Handler(DbFactory, new UpdateTest.Validator());
        DeleteTest = new DeleteTest.Handler(DbFactory);
        ListTests = new ListTests.Handler(DbFactory);
        SaveTestResult = new SaveTestResult.Handler(DbFactory, new SaveTestResult.Validator(), Students, Clock);
        DeleteTestResult = new DeleteTestResult.Handler(DbFactory);
        ListTestResults = new ListTestResults.Handler(DbFactory);
    }

    public TimeProvider Clock { get; } = new FixedClock(new DateTimeOffset(2026, 11, 2, 12, 0, 0, TimeSpan.Zero));

    public IDbContextFactory<GradesDbContext> DbFactory { get; }

    public FakeSubjects Subjects { get; } = new();

    public FakeStudents Students { get; } = new();

    public CreateGradeScale.Handler CreateScale { get; }

    public UpdateGradeScale.Handler UpdateScale { get; }

    public DeleteGradeScale.Handler DeleteScale { get; }

    public ListGradeScales.Handler ListScales { get; }

    public CreateGrade.Handler CreateGrade { get; }

    public UpdateGrade.Handler UpdateGrade { get; }

    public DeleteGrade.Handler DeleteGrade { get; }

    public ListGrades.Handler ListGrades { get; }

    public DeleteGradesFor.Handler DeleteGradesFor { get; }

    public CreateTest.Handler CreateTest { get; }

    public UpdateTest.Handler UpdateTest { get; }

    public DeleteTest.Handler DeleteTest { get; }

    public ListTests.Handler ListTests { get; }

    public SaveTestResult.Handler SaveTestResult { get; }

    public DeleteTestResult.Handler DeleteTestResult { get; }

    public ListTestResults.Handler ListTestResults { get; }

    /// <summary>Sweden's A-F: E is the lowest passing grade, F is a fail.</summary>
    public static IReadOnlyList<GradeScaleItemInput> SwedishSteps { get; } =
    [
        new(null, "A", 5, "Excellent", true),
        new(null, "B", 4, null, true),
        new(null, "C", 3, null, true),
        new(null, "D", 2, null, true),
        new(null, "E", 1, null, true),
        new(null, "F", 0, "Fail", false),
    ];

    public async Task<GradeScaleDto> AddSwedishScaleAsync(CancellationToken cancellationToken) =>
        (await CreateScale.HandleAsync(new CreateGradeScale.Command("A-F", "Sweden", null, SwedishSteps), cancellationToken)).Value;

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

    internal sealed class FakeStudents : IStudentLookup
    {
        private readonly List<StudentReference> _items = [];

        public StudentReference Add(string name)
        {
            var student = new StudentReference(Guid.NewGuid(), Guid.NewGuid(), name, null);
            _items.Add(student);
            return student;
        }

        public Task<IReadOnlyList<StudentReference>> ListAsync(CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<StudentReference>>([.. _items.OrderBy(s => s.Name)]);

        public Task<IReadOnlyList<StudentReference>> ListForSchoolAsync(Guid clientId, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<StudentReference>>([.. _items.Where(s => s.ClientId == clientId).OrderBy(s => s.Name)]);
    }

    private sealed class TestDbFactory(DbContextOptions<GradesDbContext> options) : IDbContextFactory<GradesDbContext>
    {
        public GradesDbContext CreateDbContext() => new(options);
    }

    private sealed class FixedClock(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
