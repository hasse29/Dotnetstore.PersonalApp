using Dotnetstore.PersonalApp.Modules.Email.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Dotnetstore.PersonalApp.Modules.Email.Tests;

public sealed class MigrationTests
{
    [Fact]
    public async Task MigrationsCreateTheSchemaMatchingTheModel()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync(TestContext.Current.CancellationToken);
        var options = new DbContextOptionsBuilder<EmailDbContext>().UseSqlite(connection).Options;

        await using var db = new EmailDbContext(options);
        await db.Database.MigrateAsync(TestContext.Current.CancellationToken);

        (await db.Accounts.ToListAsync(TestContext.Current.CancellationToken)).ShouldBeEmpty();
        db.Database.HasPendingModelChanges().ShouldBeFalse();
    }
}
