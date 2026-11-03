using Dotnetstore.PersonalApp.Modules.Grades.Domain;
using Microsoft.EntityFrameworkCore;

namespace Dotnetstore.PersonalApp.Modules.Grades.Persistence;

internal sealed class GradesDbContext(DbContextOptions<GradesDbContext> options) : DbContext(options)
{
    public DbSet<GradeScale> GradeScales => Set<GradeScale>();

    public DbSet<GradeScaleItem> GradeScaleItems => Set<GradeScaleItem>();

    public DbSet<Grade> Grades => Set<Grade>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(GradesDbContext).Assembly);
    }
}
