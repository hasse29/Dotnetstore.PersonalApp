using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Dotnetstore.PersonalApp.Modules.EmailAddresses.Persistence;

/// <summary>
/// Used only by `dotnet ef`; the app itself registers the context through <see cref="EmailAddressesModule"/>.
/// </summary>
internal sealed class EmailAddressesDbContextFactory : IDesignTimeDbContextFactory<EmailAddressesDbContext>
{
    public EmailAddressesDbContext CreateDbContext(string[] args) =>
        new(new DbContextOptionsBuilder<EmailAddressesDbContext>().UseSqlite("Data Source=design-time.db").Options);
}
