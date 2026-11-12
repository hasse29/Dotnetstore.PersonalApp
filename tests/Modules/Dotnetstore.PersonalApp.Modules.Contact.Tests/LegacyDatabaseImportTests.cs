using Dotnetstore.PersonalApp.Modules.Contact.Persistence;
using Dotnetstore.PersonalApp.Modules.Contact.Startup;
using Dotnetstore.PersonalApp.Shared.Localization;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Dotnetstore.PersonalApp.Modules.Contact.Tests;

public sealed class LegacyDatabaseImportTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "personalapp-contact-" + Guid.NewGuid().ToString("N"));

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, recursive: true);
        }
    }

    [Fact]
    public async Task StartupCopiesTheRowsOfTheFormerDatabasesIntoContactAndDeletesThem()
    {
        Directory.CreateDirectory(_directory);
        var legacy = Path.Combine(_directory, "addresses.db");
        var ownerId = Guid.NewGuid();
        var typeId = Guid.Parse("0197c0de-0000-7000-8000-000000000001");

        // A legacy database as the old Addresses module created it: same schema, one user-entered address.
        var oldOptions = new DbContextOptionsBuilder<ContactDbContext>().UseSqlite($"Data Source={legacy}").Options;
        await using (var old = new ContactDbContext(oldOptions))
        {
            await old.Database.MigrateAsync(Ct);
            await old.Database.ExecuteSqlRawAsync(
                "INSERT INTO Addresses (Id, ObjectId, AddressTypeId, Street1, PostalCode, City, Country) VALUES ({0}, {1}, {2}, 'Main St 1', '11122', 'Stockholm', 'Sweden')",
                [Guid.NewGuid(), ownerId, typeId],
                Ct);
        }

        var contactPath = Path.Combine(_directory, "contact.db");
        var factory = new TestFactory(new DbContextOptionsBuilder<ContactDbContext>().UseSqlite($"Data Source={contactPath}").Options);
        var task = new MigrateContactDatabaseTask(factory, new Localizer(), [legacy, Path.Combine(_directory, "missing.db")]);

        await task.RunAsync(Ct);
        SqliteConnection.ClearAllPools();

        await using var db = factory.CreateDbContext();
        var addresses = await db.Addresses.ToListAsync(Ct);
        addresses.Count.ShouldBe(1);
        addresses[0].ObjectId.ShouldBe(ownerId);
        addresses[0].City.ShouldBe("Stockholm");
        (await db.AddressTypes.CountAsync(Ct)).ShouldBe(4);
        File.Exists(legacy).ShouldBeFalse();
    }

    [Fact]
    public async Task ALegacyTypeWithTheNameOfAnExistingTypeIsMergedIntoItInsteadOfBlockingStartup()
    {
        Directory.CreateDirectory(_directory);
        var legacy = Path.Combine(_directory, "addresses.db");
        var seededHome = Guid.Parse("0197c0de-0000-7000-8000-000000000004");
        var customHome = Guid.NewGuid();

        // The user deleted the seeded "Home" type and made their own with the same name, and has an address of that type.
        var oldOptions = new DbContextOptionsBuilder<ContactDbContext>().UseSqlite($"Data Source={legacy}").Options;
        await using (var old = new ContactDbContext(oldOptions))
        {
            await old.Database.MigrateAsync(Ct);
            await old.Database.ExecuteSqlRawAsync("DELETE FROM AddressTypes WHERE Id = {0}", [seededHome], Ct);
            await old.Database.ExecuteSqlRawAsync("INSERT INTO AddressTypes (Id, Name, CreatedAt) VALUES ({0}, 'home', '2026-02-02 00:00:00+00:00')", [customHome], Ct);
            await old.Database.ExecuteSqlRawAsync(
                "INSERT INTO Addresses (Id, ObjectId, AddressTypeId, Street1, PostalCode, City, Country) VALUES ({0}, {1}, {2}, 'Main St 1', '11122', 'Stockholm', 'Sweden')",
                [Guid.NewGuid(), Guid.NewGuid(), customHome],
                Ct);
        }

        var factory = new TestFactory(new DbContextOptionsBuilder<ContactDbContext>().UseSqlite($"Data Source={Path.Combine(_directory, "contact.db")}").Options);
        var task = new MigrateContactDatabaseTask(factory, new Localizer(), [legacy]);

        await task.RunAsync(Ct);
        SqliteConnection.ClearAllPools();

        task.SkippedLegacyDatabases.ShouldBeEmpty();
        await using var db = factory.CreateDbContext();
        (await db.AddressTypes.CountAsync(Ct)).ShouldBe(4);
        (await db.Addresses.SingleAsync(Ct)).AddressTypeId.ShouldBe(seededHome);
        File.Exists(legacy).ShouldBeFalse();
    }

    [Fact]
    public async Task ADamagedLegacyFileIsKeptAndDoesNotStopTheOthersFromBeingImported()
    {
        Directory.CreateDirectory(_directory);
        var damaged = Path.Combine(_directory, "emailaddresses.db");
        await File.WriteAllTextAsync(damaged, "this is not a sqlite database, just text that is long enough to be read as a header", Ct);

        var good = Path.Combine(_directory, "addresses.db");
        var owner = Guid.NewGuid();
        var oldOptions = new DbContextOptionsBuilder<ContactDbContext>().UseSqlite($"Data Source={good}").Options;
        await using (var old = new ContactDbContext(oldOptions))
        {
            await old.Database.MigrateAsync(Ct);
            await old.Database.ExecuteSqlRawAsync(
                "INSERT INTO Addresses (Id, ObjectId, AddressTypeId, Street1, PostalCode, City, Country) VALUES ({0}, {1}, '0197C0DE-0000-7000-8000-000000000001', 'Main St 1', '11122', 'Stockholm', 'Sweden')",
                [Guid.NewGuid(), owner],
                Ct);
        }

        var factory = new TestFactory(new DbContextOptionsBuilder<ContactDbContext>().UseSqlite($"Data Source={Path.Combine(_directory, "contact.db")}").Options);
        var task = new MigrateContactDatabaseTask(factory, new Localizer(), [damaged, good]);

        await task.RunAsync(Ct);
        SqliteConnection.ClearAllPools();

        task.SkippedLegacyDatabases.ShouldBe([damaged]);
        File.Exists(damaged).ShouldBeTrue();
        File.Exists(good).ShouldBeFalse();
        await using var db = factory.CreateDbContext();
        (await db.Addresses.CountAsync(a => a.ObjectId == owner, Ct)).ShouldBe(1);
    }

    private sealed class TestFactory(DbContextOptions<ContactDbContext> options) : IDbContextFactory<ContactDbContext>
    {
        public ContactDbContext CreateDbContext() => new(options);
    }
}
