using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Dotnetstore.PersonalApp.Modules.Email.Persistence;

/// <summary>
/// Used only by `dotnet ef`; the app itself registers the context through <see cref="EmailModule"/>.
/// </summary>
internal sealed class EmailDbContextFactory : IDesignTimeDbContextFactory<EmailDbContext>
{
    public EmailDbContext CreateDbContext(string[] args) =>
        new(new DbContextOptionsBuilder<EmailDbContext>().UseSqlite("Data Source=design-time.db").Options);
}
