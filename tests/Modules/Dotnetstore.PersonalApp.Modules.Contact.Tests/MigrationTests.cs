using Dotnetstore.PersonalApp.Modules.Contact.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Dotnetstore.PersonalApp.Modules.Contact.Tests;

public sealed class MigrationTests
{
    [Fact]
    public async Task MigrationsCreateTheSchemaAndSeedTheDefaultTypes()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync(TestContext.Current.CancellationToken);
        var options = new DbContextOptionsBuilder<ContactDbContext>().UseSqlite(connection).Options;

        await using var db = new ContactDbContext(options);
        await db.Database.MigrateAsync(TestContext.Current.CancellationToken);

        var names = await db.AddressTypes.OrderBy(t => t.Name).Select(t => t.Name).ToListAsync(TestContext.Current.CancellationToken);
        names.ShouldBe(["Billing", "Delivery", "Home", "Visiting"]);
        (await db.EmailAddressTypes.AnyAsync(TestContext.Current.CancellationToken)).ShouldBeTrue();
        (await db.PhoneNumberTypes.AnyAsync(TestContext.Current.CancellationToken)).ShouldBeTrue();
        db.Database.HasPendingModelChanges().ShouldBeFalse();
    }
}
