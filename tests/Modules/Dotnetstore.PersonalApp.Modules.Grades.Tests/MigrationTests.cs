using Dotnetstore.PersonalApp.Modules.Grades.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Dotnetstore.PersonalApp.Modules.Grades.Tests;

public sealed class MigrationTests
{
    [Fact]
    public async Task MigrationsCreateTheSchemaMatchingTheModel()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync(TestContext.Current.CancellationToken);
        var options = new DbContextOptionsBuilder<GradesDbContext>().UseSqlite(connection).Options;

        await using var db = new GradesDbContext(options);
        await db.Database.MigrateAsync(TestContext.Current.CancellationToken);

        (await db.Grades.ToListAsync(TestContext.Current.CancellationToken)).ShouldBeEmpty();
        db.Database.HasPendingModelChanges().ShouldBeFalse();
    }
}
