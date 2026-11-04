using Dotnetstore.PersonalApp.Modules.TimeReports.Features.Expenses;
using Dotnetstore.PersonalApp.Modules.TimeReports.Features.TimeReports;
using Dotnetstore.PersonalApp.Modules.TimeReports.Features.WorkTimeTypes;
using Dotnetstore.PersonalApp.Modules.TimeReports.Persistence;
using Dotnetstore.PersonalApp.Shared.Clients;
using Dotnetstore.PersonalApp.Shared.Localization;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Dotnetstore.PersonalApp.Modules.TimeReports.Tests;

/// <summary>Real SQLite (in-memory, kept alive by one open connection) with the module's handlers and a fake client lookup.</summary>
internal sealed class TimeReportsFixture : IDisposable
{
    private readonly SqliteConnection _connection = new("Data Source=:memory:");

    public TimeReportsFixture()
    {
        _connection.Open();
        DbFactory = new TestDbFactory(new DbContextOptionsBuilder<TimeReportsDbContext>().UseSqlite(_connection).Options);
        using var db = DbFactory.CreateDbContext();
        db.Database.EnsureCreated();

        Create = new CreateTimeReport.Handler(DbFactory, new CreateTimeReport.Validator(), Clients, Clock);
        Update = new UpdateTimeReport.Handler(DbFactory, new UpdateTimeReport.Validator(), Clients);
        Delete = new DeleteTimeReport.Handler(DbFactory);
        List = new ListTimeReports.Handler(DbFactory, Clients);
        Export = new ExportTimeReports.Handler(DbFactory, Clients, new Localizer());
        ExportKilometers = new ExportKilometerSummary.Handler(DbFactory, Clients, new Localizer());
        CreateExpense = new CreateExpense.Handler(DbFactory, new CreateExpense.Validator(), Clients, Clock);
        UpdateExpense = new UpdateExpense.Handler(DbFactory, new UpdateExpense.Validator(), Clients);
        DeleteExpense = new DeleteExpense.Handler(DbFactory);
        ListExpenses = new ListExpenses.Handler(DbFactory, Clients);
        GetReceipt = new GetExpenseReceipt.Handler(DbFactory);
        ExportExpenses = new ExportExpenses.Handler(DbFactory, Clients, new Localizer());
        CreateType = new CreateWorkTimeType.Handler(DbFactory, new CreateWorkTimeType.Validator(), Clock);
        UpdateType = new UpdateWorkTimeType.Handler(DbFactory, new UpdateWorkTimeType.Validator());
        DeleteType = new DeleteWorkTimeType.Handler(DbFactory);
        ListTypes = new ListWorkTimeTypes.Handler(DbFactory);
    }

    public TimeProvider Clock { get; } = new FixedClock(new DateTimeOffset(2026, 10, 31, 12, 0, 0, TimeSpan.Zero));

    public FakeClients Clients { get; } = new();

    public IDbContextFactory<TimeReportsDbContext> DbFactory { get; }

    public CreateTimeReport.Handler Create { get; }

    public UpdateTimeReport.Handler Update { get; }

    public DeleteTimeReport.Handler Delete { get; }

    public ListTimeReports.Handler List { get; }

    public ExportTimeReports.Handler Export { get; }

    public ExportKilometerSummary.Handler ExportKilometers { get; }

    public CreateExpense.Handler CreateExpense { get; }

    public UpdateExpense.Handler UpdateExpense { get; }

    public DeleteExpense.Handler DeleteExpense { get; }

    public ListExpenses.Handler ListExpenses { get; }

    public GetExpenseReceipt.Handler GetReceipt { get; }

    public ExportExpenses.Handler ExportExpenses { get; }

    public CreateWorkTimeType.Handler CreateType { get; }

    public UpdateWorkTimeType.Handler UpdateType { get; }

    public DeleteWorkTimeType.Handler DeleteType { get; }

    public ListWorkTimeTypes.Handler ListTypes { get; }

    public static CreateTimeReport.Command ValidCommand(Guid userId, Guid clientId) =>
        new(userId, clientId, WorkTimeTypeSeed.WorkId, new DateOnly(2026, 10, 30), new TimeOnly(8, 0), new TimeOnly(16, 30), 8m, "Stockholm", 42.5m);

    public void Dispose() => _connection.Dispose();

    internal sealed class FakeClients : IClientLookup
    {
        private readonly List<ClientReference> _clients = [];

        public ClientReference Add(string name)
        {
            var client = new ClientReference(Guid.NewGuid(), name);
            _clients.Add(client);
            return client;
        }

        public Task<IReadOnlyList<ClientReference>> ListAsync(CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<ClientReference>>([.. _clients]);
    }

    private sealed class TestDbFactory(DbContextOptions<TimeReportsDbContext> options) : IDbContextFactory<TimeReportsDbContext>
    {
        public TimeReportsDbContext CreateDbContext() => new(options);
    }

    private sealed class FixedClock(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}