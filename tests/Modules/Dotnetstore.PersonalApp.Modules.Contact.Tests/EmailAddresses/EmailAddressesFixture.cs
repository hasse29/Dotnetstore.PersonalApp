using Dotnetstore.PersonalApp.Modules.Contact.EmailAddresses;
using Dotnetstore.PersonalApp.Modules.Contact.Persistence;
using Dotnetstore.PersonalApp.Modules.Contact.EmailAddresses.Features.EmailAddresses;
using Dotnetstore.PersonalApp.Modules.Contact.EmailAddresses.Features.EmailAddressTypes;
using Dotnetstore.PersonalApp.Modules.Contact.EmailAddresses.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Dotnetstore.PersonalApp.Modules.Contact.Tests.EmailAddresses;

/// <summary>
/// Real SQLite (in-memory, kept alive by one open connection) with the module's handlers wired up.
/// The schema is created from the model, which includes the seeded email address types.
/// </summary>
internal sealed class EmailAddressesFixture : IDisposable
{
    private readonly SqliteConnection _connection = new("Data Source=:memory:");

    public EmailAddressesFixture()
    {
        _connection.Open();
        DbFactory = new TestDbFactory(new DbContextOptionsBuilder<ContactDbContext>().UseSqlite(_connection).Options);
        using var db = DbFactory.CreateDbContext();
        db.Database.EnsureCreated();

        Create = new CreateEmailAddressType.Handler(DbFactory, new CreateEmailAddressType.Validator(), Clock);
        Get = new GetEmailAddressType.Handler(DbFactory);
        List = new ListEmailAddressTypes.Handler(DbFactory);
        Update = new UpdateEmailAddressType.Handler(DbFactory, new UpdateEmailAddressType.Validator());
        Delete = new DeleteEmailAddressType.Handler(DbFactory);

        CreateEmailAddress = new CreateEmailAddress.Handler(DbFactory, new CreateEmailAddress.Validator());
        GetEmailAddress = new GetEmailAddress.Handler(DbFactory);
        ListEmailAddresses = new ListEmailAddresses.Handler(DbFactory);
        UpdateEmailAddress = new UpdateEmailAddress.Handler(DbFactory, new UpdateEmailAddress.Validator());
        DeleteEmailAddress = new DeleteEmailAddress.Handler(DbFactory);
        DeleteEmailAddressesForObject = new DeleteEmailAddressesForObject.Handler(DbFactory);
    }

    public CreateEmailAddress.Handler CreateEmailAddress { get; }

    public GetEmailAddress.Handler GetEmailAddress { get; }

    public ListEmailAddresses.Handler ListEmailAddresses { get; }

    public UpdateEmailAddress.Handler UpdateEmailAddress { get; }

    public DeleteEmailAddress.Handler DeleteEmailAddress { get; }

    public DeleteEmailAddressesForObject.Handler DeleteEmailAddressesForObject { get; }

    /// <summary>Id of the seeded "Billing" type.</summary>
    public Guid BillingTypeId => TypeId("Billing");

    public Guid WorkTypeId => TypeId("Work");

    public CreateEmailAddress.Command ValidEmailAddress(Guid objectId, Guid? typeId = null, string email = "ada@example.com") =>
        new(objectId, typeId ?? BillingTypeId, email);

    private Guid TypeId(string name)
    {
        using var db = DbFactory.CreateDbContext();
        return db.EmailAddressTypes.Single(t => t.Name == name).Id;
    }

    public TimeProvider Clock { get; } = new FixedClock(new DateTimeOffset(2026, 10, 31, 12, 0, 0, TimeSpan.Zero));

    public IDbContextFactory<ContactDbContext> DbFactory { get; }

    public CreateEmailAddressType.Handler Create { get; }

    public GetEmailAddressType.Handler Get { get; }

    public ListEmailAddressTypes.Handler List { get; }

    public UpdateEmailAddressType.Handler Update { get; }

    public DeleteEmailAddressType.Handler Delete { get; }

    public void Dispose() => _connection.Dispose();

    private sealed class TestDbFactory(DbContextOptions<ContactDbContext> options) : IDbContextFactory<ContactDbContext>
    {
        public ContactDbContext CreateDbContext() => new(options);
    }

    private sealed class FixedClock(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
