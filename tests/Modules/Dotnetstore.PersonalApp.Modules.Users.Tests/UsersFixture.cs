using Dotnetstore.PersonalApp.Modules.Users.Domain;
using Dotnetstore.PersonalApp.Modules.Users.Features.Users;
using Dotnetstore.PersonalApp.Modules.Users.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Dotnetstore.PersonalApp.Modules.Users.Tests;

/// <summary>
/// Real SQLite (in-memory, kept alive by one open connection) with the module's handlers wired up.
/// </summary>
internal sealed class UsersFixture : IDisposable
{
    private readonly SqliteConnection _connection = new("Data Source=:memory:");

    public UsersFixture()
    {
        _connection.Open();
        DbFactory = new TestDbFactory(new DbContextOptionsBuilder<UsersDbContext>().UseSqlite(_connection).Options);
        using var db = DbFactory.CreateDbContext();
        db.Database.EnsureCreated();

        var hasher = new PasswordHasher<User>();
        Create = new CreateUser.Handler(DbFactory, new CreateUser.Validator(Clock), hasher, Clock);
        Get = new GetUser.Handler(DbFactory);
        List = new ListUsers.Handler(DbFactory);
        Update = new UpdateUser.Handler(DbFactory, new UpdateUser.Validator(Clock), hasher);
        Delete = new DeleteUser.Handler(DbFactory, Events);
    }

    public TimeProvider Clock { get; } = new FixedClock(new DateTimeOffset(2026, 10, 31, 12, 0, 0, TimeSpan.Zero));

    public IDbContextFactory<UsersDbContext> DbFactory { get; }

    public CreateUser.Handler Create { get; }

    public GetUser.Handler Get { get; }

    public ListUsers.Handler List { get; }

    public UpdateUser.Handler Update { get; }

    public DeleteUser.Handler Delete { get; }

    public static CreateUser.Command ValidCommand(string username = "ada@example.com") =>
        new("Ada", "Lovelace", username, "correct horse battery", new DateOnly(1990, 12, 10));

    public RecordingEventPublisher Events { get; } = new();

    public void Dispose() => _connection.Dispose();

    private sealed class TestDbFactory(DbContextOptions<UsersDbContext> options) : IDbContextFactory<UsersDbContext>
    {
        public UsersDbContext CreateDbContext() => new(options);
    }

    private sealed class FixedClock(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
