using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Dotnetstore.PersonalApp.Modules.Grades.Persistence;

/// <summary>
/// Used only by `dotnet ef`; the app itself registers the context through <see cref="GradesModule"/>.
/// </summary>
internal sealed class GradesDbContextFactory : IDesignTimeDbContextFactory<GradesDbContext>
{
    public GradesDbContext CreateDbContext(string[] args) =>
        new(new DbContextOptionsBuilder<GradesDbContext>().UseSqlite("Data Source=design-time.db").Options);
}
