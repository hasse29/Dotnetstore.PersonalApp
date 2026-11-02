using Dotnetstore.PersonalApp.Modules.Clients.Domain;
using Microsoft.EntityFrameworkCore;

namespace Dotnetstore.PersonalApp.Modules.Clients.Persistence;

internal sealed class ClientsDbContext(DbContextOptions<ClientsDbContext> options) : DbContext(options)
{
    public DbSet<Client> Clients => Set<Client>();

    public DbSet<ContactPerson> ContactPersons => Set<ContactPerson>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ClientsDbContext).Assembly);
    }
}
