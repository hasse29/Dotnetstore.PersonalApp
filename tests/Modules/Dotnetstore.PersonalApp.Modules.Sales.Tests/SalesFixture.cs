using Dotnetstore.PersonalApp.Modules.Sales.Features.Customers;
using Dotnetstore.PersonalApp.Modules.Sales.Features.Offers;
using Dotnetstore.PersonalApp.Modules.Sales.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Dotnetstore.PersonalApp.Modules.Sales.Tests;

/// <summary>Real SQLite (in-memory, kept alive by one open connection) with the module's handlers.</summary>
internal sealed class SalesFixture : IDisposable
{
    private readonly SqliteConnection _connection = new("Data Source=:memory:");

    public SalesFixture()
    {
        _connection.Open();
        DbFactory = new TestDbFactory(new DbContextOptionsBuilder<SalesDbContext>().UseSqlite(_connection).Options);
        using var db = DbFactory.CreateDbContext();
        db.Database.EnsureCreated();

        CreateCustomer = new CreateCustomer.Handler(DbFactory, new CreateCustomer.Validator(), Clock);
        UpdateCustomer = new UpdateCustomer.Handler(DbFactory, new UpdateCustomer.Validator());
        DeleteCustomer = new DeleteCustomer.Handler(DbFactory);
        ListCustomers = new ListCustomers.Handler(DbFactory);
        CreateOffer = new CreateOffer.Handler(DbFactory, new CreateOffer.Validator(), Clock);
        UpdateOffer = new UpdateOffer.Handler(DbFactory, new UpdateOffer.Validator(), Clock);
        ChangeStatus = new ChangeOfferStatus.Handler(DbFactory, Clock);
        DeleteOffer = new DeleteOffer.Handler(DbFactory);
        GetOffer = new GetOffer.Handler(DbFactory, Clock);
        ListOffers = new ListOffers.Handler(DbFactory, Clock);
    }

    public TimeProvider Clock { get; } = new FixedClock(new DateTimeOffset(2026, 11, 10, 12, 0, 0, TimeSpan.Zero));

    public IDbContextFactory<SalesDbContext> DbFactory { get; }

    public CreateCustomer.Handler CreateCustomer { get; }

    public UpdateCustomer.Handler UpdateCustomer { get; }

    public DeleteCustomer.Handler DeleteCustomer { get; }

    public ListCustomers.Handler ListCustomers { get; }

    public CreateOffer.Handler CreateOffer { get; }

    public UpdateOffer.Handler UpdateOffer { get; }

    public ChangeOfferStatus.Handler ChangeStatus { get; }

    public DeleteOffer.Handler DeleteOffer { get; }

    public GetOffer.Handler GetOffer { get; }

    public ListOffers.Handler ListOffers { get; }

    public static CustomerFields Customer(string name = "Acme School") =>
        new(name, "556677-8899", "office@acme.example", "08-123 45", "Main Street 1", "111 22", "Stockholm", null, true);

    public static OfferLineFields Line(string description = "Cat6 cable 2m", decimal quantity = 10, decimal price = 50, decimal vat = 25, decimal discount = 0) =>
        new(null, description, quantity, "Piece", price, vat, discount);

    public static OfferFields Offer(Guid customerId, params OfferLineFields[] lines) =>
        new(customerId, new DateOnly(2026, 11, 10), new DateOnly(2026, 12, 10), "Anna", null, lines);

    public async Task<CustomerDto> NewCustomerAsync(string name = "Acme School") =>
        (await CreateCustomer.HandleAsync(new CreateCustomer.Command(Customer(name)), CancellationToken.None)).Value;

    public async Task<OfferDetailDto> NewOfferAsync(Guid customerId, params OfferLineFields[] lines) =>
        (await CreateOffer.HandleAsync(new CreateOffer.Command(Offer(customerId, lines)), CancellationToken.None)).Value;

    public void Dispose() => _connection.Dispose();

    private sealed class TestDbFactory(DbContextOptions<SalesDbContext> options) : IDbContextFactory<SalesDbContext>
    {
        public SalesDbContext CreateDbContext() => new(options);
    }

    private sealed class FixedClock(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;

        public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;
    }
}
