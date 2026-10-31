using Dotnetstore.PersonalApp.Modules.Cvs.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Dotnetstore.PersonalApp.Modules.Cvs.Tests;

public sealed class MigrationTests
{
    [Fact]
    public async Task MigrationsCreateTheSchemaMatchingTheModel()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync(TestContext.Current.CancellationToken);
        var options = new DbContextOptionsBuilder<CvsDbContext>().UseSqlite(connection).Options;

        await using var db = new CvsDbContext(options);
        await db.Database.MigrateAsync(TestContext.Current.CancellationToken);

        Assert.Empty(await db.Cvs.ToListAsync(TestContext.Current.CancellationToken));
        Assert.False(db.Database.HasPendingModelChanges());
    }
}
