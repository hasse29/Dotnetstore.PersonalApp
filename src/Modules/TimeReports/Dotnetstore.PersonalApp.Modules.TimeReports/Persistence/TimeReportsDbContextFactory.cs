using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Dotnetstore.PersonalApp.Modules.TimeReports.Persistence;

/// <summary>
/// Used only by `dotnet ef`; the app itself registers the context through <see cref="TimeReportsModule"/>.
/// </summary>
internal sealed class TimeReportsDbContextFactory : IDesignTimeDbContextFactory<TimeReportsDbContext>
{
    public TimeReportsDbContext CreateDbContext(string[] args) =>
        new(new DbContextOptionsBuilder<TimeReportsDbContext>().UseSqlite("Data Source=design-time.db").Options);
}