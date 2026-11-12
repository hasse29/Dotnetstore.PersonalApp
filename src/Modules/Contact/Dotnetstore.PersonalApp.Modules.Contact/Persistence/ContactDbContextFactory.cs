using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Dotnetstore.PersonalApp.Modules.Contact.Persistence;

/// <summary>
/// Used only by `dotnet ef`; the app itself registers the context through <see cref="ContactModule"/>.
/// </summary>
internal sealed class ContactDbContextFactory : IDesignTimeDbContextFactory<ContactDbContext>
{
    public ContactDbContext CreateDbContext(string[] args) =>
        new(new DbContextOptionsBuilder<ContactDbContext>().UseSqlite("Data Source=design-time.db").Options);
}
