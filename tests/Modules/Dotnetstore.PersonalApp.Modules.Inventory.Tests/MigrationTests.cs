using Dotnetstore.PersonalApp.Modules.Inventory.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Dotnetstore.PersonalApp.Modules.Inventory.Tests;

public sealed class MigrationTests
{
    [Fact]
    public async Task MigrationsCreateTheSchemaMatchingTheModel()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync(TestContext.Current.CancellationToken);
        var options = new DbContextOptionsBuilder<InventoryDbContext>().UseSqlite(connection).Options;

        await using var db = new InventoryDbContext(options);
        await db.Database.MigrateAsync(TestContext.Current.CancellationToken);

        (await db.Warehouses.ToListAsync(TestContext.Current.CancellationToken)).ShouldNotBeNull();
        db.Database.HasPendingModelChanges().ShouldBeFalse();
    }
}