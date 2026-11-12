using Dotnetstore.PersonalApp.Modules.Contact.Addresses;
using Dotnetstore.PersonalApp.Modules.Contact.Persistence;
using Dotnetstore.PersonalApp.Modules.Contact.Addresses.Features.Addresses;
using Dotnetstore.PersonalApp.Modules.Contact.Addresses.Features.AddressTypes;
using Dotnetstore.PersonalApp.Modules.Contact.Addresses.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Dotnetstore.PersonalApp.Modules.Contact.Tests.Addresses;

/// <summary>
/// Real SQLite (in-memory, kept alive by one open connection) with the module's handlers wired up.
/// The schema is created from the model, which includes the seeded address types.
/// </summary>
internal sealed class AddressesFixture : IDisposable
{
    private readonly SqliteConnection _connection = new("Data Source=:memory:");

    public AddressesFixture()
    {
        _connection.Open();
        DbFactory = new TestDbFactory(new DbContextOptionsBuilder<ContactDbContext>().UseSqlite(_connection).Options);
        using var db = DbFactory.CreateDbContext();
        db.Database.EnsureCreated();

        Create = new CreateAddressType.Handler(DbFactory, new CreateAddressType.Validator(), Clock);
        Get = new GetAddressType.Handler(DbFactory);
        List = new ListAddressTypes.Handler(DbFactory);
        Update = new UpdateAddressType.Handler(DbFactory, new UpdateAddressType.Validator());
        Delete = new DeleteAddressType.Handler(DbFactory);

        CreateAddress = new CreateAddress.Handler(DbFactory, new CreateAddress.Validator());
        GetAddress = new GetAddress.Handler(DbFactory);
        ListAddresses = new ListAddresses.Handler(DbFactory);
        UpdateAddress = new UpdateAddress.Handler(DbFactory, new UpdateAddress.Validator());
        DeleteAddress = new DeleteAddress.Handler(DbFactory);
        DeleteAddressesForObject = new DeleteAddressesForObject.Handler(DbFactory);
    }

    public CreateAddress.Handler CreateAddress { get; }

    public GetAddress.Handler GetAddress { get; }

    public ListAddresses.Handler ListAddresses { get; }

    public UpdateAddress.Handler UpdateAddress { get; }

    public DeleteAddress.Handler DeleteAddress { get; }

    public DeleteAddressesForObject.Handler DeleteAddressesForObject { get; }

    /// <summary>Id of the seeded "Billing" type.</summary>
    public Guid BillingTypeId => TypeId("Billing");

    public Guid DeliveryTypeId => TypeId("Delivery");

    public CreateAddress.Command ValidAddress(Guid objectId, Guid? typeId = null, string city = "Stockholm") =>
        new(objectId, typeId ?? BillingTypeId, "Ada Lovelace", "Drottninggatan 1", "Floor 3", "111 51", city, null, "Sweden");

    private Guid TypeId(string name)
    {
        using var db = DbFactory.CreateDbContext();
        return db.AddressTypes.Single(t => t.Name == name).Id;
    }

    public TimeProvider Clock { get; } = new FixedClock(new DateTimeOffset(2026, 10, 31, 12, 0, 0, TimeSpan.Zero));

    public IDbContextFactory<ContactDbContext> DbFactory { get; }

    public CreateAddressType.Handler Create { get; }

    public GetAddressType.Handler Get { get; }

    public ListAddressTypes.Handler List { get; }

    public UpdateAddressType.Handler Update { get; }

    public DeleteAddressType.Handler Delete { get; }

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
