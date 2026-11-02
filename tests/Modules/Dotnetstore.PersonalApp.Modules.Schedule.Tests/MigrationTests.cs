using Dotnetstore.PersonalApp.Modules.Schedule.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Dotnetstore.PersonalApp.Modules.Schedule.Tests;

public sealed class MigrationTests
{
    [Fact]
    public async Task MigrationsCreateTheSchemaMatchingTheModel()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync(TestContext.Current.CancellationToken);
        var options = new DbContextOptionsBuilder<ScheduleDbContext>().UseSqlite(connection).Options;

        await using var db = new ScheduleDbContext(options);
        await db.Database.MigrateAsync(TestContext.Current.CancellationToken);

        (await db.ScheduleEntries.ToListAsync(TestContext.Current.CancellationToken)).ShouldBeEmpty();
        db.Database.HasPendingModelChanges().ShouldBeFalse();
    }
}
