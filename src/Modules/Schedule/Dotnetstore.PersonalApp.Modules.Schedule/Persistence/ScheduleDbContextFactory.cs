using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Dotnetstore.PersonalApp.Modules.Schedule.Persistence;

/// <summary>
/// Used only by `dotnet ef`; the app itself registers the context through <see cref="ScheduleModule"/>.
/// </summary>
internal sealed class ScheduleDbContextFactory : IDesignTimeDbContextFactory<ScheduleDbContext>
{
    public ScheduleDbContext CreateDbContext(string[] args) =>
        new(new DbContextOptionsBuilder<ScheduleDbContext>().UseSqlite("Data Source=design-time.db").Options);
}
