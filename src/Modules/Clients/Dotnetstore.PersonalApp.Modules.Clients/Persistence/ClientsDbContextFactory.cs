using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Dotnetstore.PersonalApp.Modules.Clients.Persistence;

/// <summary>
/// Used only by `dotnet ef`; the app itself registers the context through <see cref="ClientsModule"/>.
/// </summary>
internal sealed class ClientsDbContextFactory : IDesignTimeDbContextFactory<ClientsDbContext>
{
    public ClientsDbContext CreateDbContext(string[] args) =>
        new(new DbContextOptionsBuilder<ClientsDbContext>().UseSqlite("Data Source=design-time.db").Options);
}
