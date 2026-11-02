using Dotnetstore.PersonalApp.Modules.Subjects.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Dotnetstore.PersonalApp.Modules.Subjects.Tests;

public sealed class MigrationTests
{
    [Fact]
    public async Task MigrationsCreateTheSchemaMatchingTheModel()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync(TestContext.Current.CancellationToken);
        var options = new DbContextOptionsBuilder<SubjectsDbContext>().UseSqlite(connection).Options;

        await using var db = new SubjectsDbContext(options);
        await db.Database.MigrateAsync(TestContext.Current.CancellationToken);

        (await db.Subjects.ToListAsync(TestContext.Current.CancellationToken)).ShouldBeEmpty();
        db.Database.HasPendingModelChanges().ShouldBeFalse();
    }
}
