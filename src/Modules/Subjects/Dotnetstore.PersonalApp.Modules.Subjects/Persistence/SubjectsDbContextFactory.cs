using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Dotnetstore.PersonalApp.Modules.Subjects.Persistence;

/// <summary>
/// Used only by `dotnet ef`; the app itself registers the context through <see cref="SubjectsModule"/>.
/// </summary>
internal sealed class SubjectsDbContextFactory : IDesignTimeDbContextFactory<SubjectsDbContext>
{
    public SubjectsDbContext CreateDbContext(string[] args) =>
        new(new DbContextOptionsBuilder<SubjectsDbContext>().UseSqlite("Data Source=design-time.db").Options);
}
