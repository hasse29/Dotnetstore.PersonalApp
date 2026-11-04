using Dotnetstore.PersonalApp.Modules.Purchasing.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Dotnetstore.PersonalApp.Modules.Purchasing.Tests;

public sealed class MigrationTests
{
    [Fact]
    public async Task MigrationsCreateTheSchemaMatchingTheModel()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync(TestContext.Current.CancellationToken);
        var options = new DbContextOptionsBuilder<PurchasingDbContext>().UseSqlite(connection).Options;

        await using var db = new PurchasingDbContext(options);
        await db.Database.MigrateAsync(TestContext.Current.CancellationToken);

        (await db.ShipMethods.ToListAsync(TestContext.Current.CancellationToken)).ShouldNotBeNull();
        db.Database.HasPendingModelChanges().ShouldBeFalse();
    }
}