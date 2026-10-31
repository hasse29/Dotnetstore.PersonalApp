using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Dotnetstore.PersonalApp.Modules.PhoneNumbers.Persistence;

/// <summary>
/// Used only by `dotnet ef`; the app itself registers the context through <see cref="PhoneNumbersModule"/>.
/// </summary>
internal sealed class PhoneNumbersDbContextFactory : IDesignTimeDbContextFactory<PhoneNumbersDbContext>
{
    public PhoneNumbersDbContext CreateDbContext(string[] args) =>
        new(new DbContextOptionsBuilder<PhoneNumbersDbContext>().UseSqlite("Data Source=design-time.db").Options);
}
