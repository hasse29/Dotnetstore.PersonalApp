using Dotnetstore.PersonalApp.Modules.Clients.Features.Clients;
using Dotnetstore.PersonalApp.Modules.Clients.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Dotnetstore.PersonalApp.Modules.Clients.Tests;

/// <summary>
/// Real SQLite (in-memory, kept alive by one open connection) with the module's handlers wired up.
/// </summary>
internal sealed class ClientsFixture : IDisposable
{
    private readonly SqliteConnection _connection = new("Data Source=:memory:");

    public ClientsFixture()
    {
        _connection.Open();
        DbFactory = new TestDbFactory(new DbContextOptionsBuilder<ClientsDbContext>().UseSqlite(_connection).Options);
        using var db = DbFactory.CreateDbContext();
        db.Database.EnsureCreated();

        var createValidator = new CreateClient.Validator();
        var updateValidator = new UpdateClient.Validator();
        Create = new CreateClient.Handler(DbFactory, createValidator, Clock);
        Get = new GetClient.Handler(DbFactory);
        List = new ListClients.Handler(DbFactory);
        Update = new UpdateClient.Handler(DbFactory, updateValidator);
        Delete = new DeleteClient.Handler(DbFactory, Events);
    }

    public TimeProvider Clock { get; } = new FixedClock(new DateTimeOffset(2026, 10, 31, 12, 0, 0, TimeSpan.Zero));

    public IDbContextFactory<ClientsDbContext> DbFactory { get; }

    public CreateClient.Handler Create { get; }

    public GetClient.Handler Get { get; }

    public ListClients.Handler List { get; }

    public UpdateClient.Handler Update { get; }

    public DeleteClient.Handler Delete { get; }

    public static CreateClient.Command ValidCommand(string organizationNumber = "556677-8899", string name = "Acme AB") =>
        new(name, organizationNumber, "SE556677889901", "info@acme.example", "+46 8 123 456", "https://acme.example", "Key account");

    public RecordingEventPublisher Events { get; } = new();

    public void Dispose() => _connection.Dispose();

    private sealed class TestDbFactory(DbContextOptions<ClientsDbContext> options) : IDbContextFactory<ClientsDbContext>
    {
        public ClientsDbContext CreateDbContext() => new(options);
    }

    private sealed class FixedClock(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
