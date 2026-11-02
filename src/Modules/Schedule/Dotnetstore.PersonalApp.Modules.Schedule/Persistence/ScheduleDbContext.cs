using Dotnetstore.PersonalApp.Modules.Schedule.Domain;
using Microsoft.EntityFrameworkCore;

namespace Dotnetstore.PersonalApp.Modules.Schedule.Persistence;

internal sealed class ScheduleDbContext(DbContextOptions<ScheduleDbContext> options) : DbContext(options)
{
    public DbSet<ScheduleEntry> ScheduleEntries => Set<ScheduleEntry>();

    public DbSet<ScheduleEntryStudent> ScheduleEntryStudents => Set<ScheduleEntryStudent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ScheduleDbContext).Assembly);
    }
}
