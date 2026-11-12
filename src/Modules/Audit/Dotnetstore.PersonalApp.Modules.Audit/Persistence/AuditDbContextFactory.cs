using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Dotnetstore.PersonalApp.Modules.Audit.Persistence;

/// <summary>
/// Used only by `dotnet ef`; the app itself registers the context through <see cref="AuditModule"/>.
/// </summary>
internal sealed class AuditDbContextFactory : IDesignTimeDbContextFactory<AuditDbContext>
{
    public AuditDbContext CreateDbContext(string[] args) =>
        new(new DbContextOptionsBuilder<AuditDbContext>().UseSqlite("Data Source=design-time.db").Options);
}
