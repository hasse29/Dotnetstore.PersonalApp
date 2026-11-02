using Dotnetstore.PersonalApp.Modules.PhoneNumbers.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Dotnetstore.PersonalApp.Modules.PhoneNumbers.Tests;

public sealed class MigrationTests
{
    [Fact]
    public async Task MigrationsCreateTheSchemaAndSeedTheDefaultTypes()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync(TestContext.Current.CancellationToken);
        var options = new DbContextOptionsBuilder<PhoneNumbersDbContext>().UseSqlite(connection).Options;

        await using var db = new PhoneNumbersDbContext(options);
        await db.Database.MigrateAsync(TestContext.Current.CancellationToken);

        var names = await db.PhoneNumberTypes.OrderBy(t => t.Name).Select(t => t.Name).ToListAsync(TestContext.Current.CancellationToken);
        names.ShouldBe(["Home", "Mobile", "Work"]);
        db.Database.HasPendingModelChanges().ShouldBeFalse();
    }
}
