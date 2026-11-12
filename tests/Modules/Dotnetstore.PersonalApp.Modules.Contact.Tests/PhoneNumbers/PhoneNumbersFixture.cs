using Dotnetstore.PersonalApp.Modules.Contact.PhoneNumbers;
using Dotnetstore.PersonalApp.Modules.Contact.Persistence;
using Dotnetstore.PersonalApp.Modules.Contact.PhoneNumbers.Features.PhoneNumbers;
using Dotnetstore.PersonalApp.Modules.Contact.PhoneNumbers.Features.PhoneNumberTypes;
using Dotnetstore.PersonalApp.Modules.Contact.PhoneNumbers.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Dotnetstore.PersonalApp.Modules.Contact.Tests.PhoneNumbers;

/// <summary>
/// Real SQLite (in-memory, kept alive by one open connection) with the module's handlers wired up.
/// The schema is created from the model, which includes the seeded phone number types.
/// </summary>
internal sealed class PhoneNumbersFixture : IDisposable
{
    private readonly SqliteConnection _connection = new("Data Source=:memory:");

    public PhoneNumbersFixture()
    {
        _connection.Open();
        DbFactory = new TestDbFactory(new DbContextOptionsBuilder<ContactDbContext>().UseSqlite(_connection).Options);
        using var db = DbFactory.CreateDbContext();
        db.Database.EnsureCreated();

        Create = new CreatePhoneNumberType.Handler(DbFactory, new CreatePhoneNumberType.Validator(), Clock);
        Get = new GetPhoneNumberType.Handler(DbFactory);
        List = new ListPhoneNumberTypes.Handler(DbFactory);
        Update = new UpdatePhoneNumberType.Handler(DbFactory, new UpdatePhoneNumberType.Validator());
        Delete = new DeletePhoneNumberType.Handler(DbFactory);

        CreatePhoneNumber = new CreatePhoneNumber.Handler(DbFactory, new CreatePhoneNumber.Validator());
        GetPhoneNumber = new GetPhoneNumber.Handler(DbFactory);
        ListPhoneNumbers = new ListPhoneNumbers.Handler(DbFactory);
        UpdatePhoneNumber = new UpdatePhoneNumber.Handler(DbFactory, new UpdatePhoneNumber.Validator());
        DeletePhoneNumber = new DeletePhoneNumber.Handler(DbFactory);
        DeletePhoneNumbersForObject = new DeletePhoneNumbersForObject.Handler(DbFactory);
    }

    public CreatePhoneNumber.Handler CreatePhoneNumber { get; }

    public GetPhoneNumber.Handler GetPhoneNumber { get; }

    public ListPhoneNumbers.Handler ListPhoneNumbers { get; }

    public UpdatePhoneNumber.Handler UpdatePhoneNumber { get; }

    public DeletePhoneNumber.Handler DeletePhoneNumber { get; }

    public DeletePhoneNumbersForObject.Handler DeletePhoneNumbersForObject { get; }

    /// <summary>Id of the seeded "Mobile" type.</summary>
    public Guid MobileTypeId => TypeId("Mobile");

    public Guid WorkTypeId => TypeId("Work");

    public CreatePhoneNumber.Command ValidPhoneNumber(Guid objectId, Guid? typeId = null, string number = "+46 8 123 456") =>
        new(objectId, typeId ?? MobileTypeId, number);

    private Guid TypeId(string name)
    {
        using var db = DbFactory.CreateDbContext();
        return db.PhoneNumberTypes.Single(t => t.Name == name).Id;
    }

    public TimeProvider Clock { get; } = new FixedClock(new DateTimeOffset(2026, 10, 31, 12, 0, 0, TimeSpan.Zero));

    public IDbContextFactory<ContactDbContext> DbFactory { get; }

    public CreatePhoneNumberType.Handler Create { get; }

    public GetPhoneNumberType.Handler Get { get; }

    public ListPhoneNumberTypes.Handler List { get; }

    public UpdatePhoneNumberType.Handler Update { get; }

    public DeletePhoneNumberType.Handler Delete { get; }

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
