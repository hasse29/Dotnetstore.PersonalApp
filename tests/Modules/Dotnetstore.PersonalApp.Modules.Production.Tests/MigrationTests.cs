using Dotnetstore.PersonalApp.Modules.Production.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Dotnetstore.PersonalApp.Modules.Production.Tests;

public sealed class MigrationTests
{
    [Fact]
    public async Task MigrationsCreateTheSchemaMatchingTheModel()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync(TestContext.Current.CancellationToken);
        var options = new DbContextOptionsBuilder<ProductionDbContext>().UseSqlite(connection).Options;

        await using var db = new ProductionDbContext(options);
        await db.Database.MigrateAsync(TestContext.Current.CancellationToken);

        (await db.UnitMeasures.ToListAsync(TestContext.Current.CancellationToken)).ShouldNotBeNull();
        db.Database.HasPendingModelChanges().ShouldBeFalse();
    }
}