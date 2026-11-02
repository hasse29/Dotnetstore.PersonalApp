using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Dotnetstore.PersonalApp.Modules.Students.Persistence;

/// <summary>
/// Used only by `dotnet ef`; the app itself registers the context through <see cref="StudentsModule"/>.
/// </summary>
internal sealed class StudentsDbContextFactory : IDesignTimeDbContextFactory<StudentsDbContext>
{
    public StudentsDbContext CreateDbContext(string[] args) =>
        new(new DbContextOptionsBuilder<StudentsDbContext>().UseSqlite("Data Source=design-time.db").Options);
}
