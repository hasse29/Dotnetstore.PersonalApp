using Dotnetstore.PersonalApp.Modules.EmailAddresses.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Dotnetstore.PersonalApp.Modules.EmailAddresses.Tests;

public sealed class MigrationTests
{
    [Fact]
    public async Task MigrationsCreateTheSchemaAndSeedTheDefaultTypes()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync(TestContext.Current.CancellationToken);
        var options = new DbContextOptionsBuilder<EmailAddressesDbContext>().UseSqlite(connection).Options;

        await using var db = new EmailAddressesDbContext(options);
        await db.Database.MigrateAsync(TestContext.Current.CancellationToken);

        var names = await db.EmailAddressTypes.OrderBy(t => t.Name).Select(t => t.Name).ToListAsync(TestContext.Current.CancellationToken);
        Assert.Equal(["Billing", "Private", "Work"], names);
        Assert.False(db.Database.HasPendingModelChanges());
    }
}
