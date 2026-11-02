using Dotnetstore.PersonalApp.Modules.Subjects.Domain;
using Microsoft.EntityFrameworkCore;

namespace Dotnetstore.PersonalApp.Modules.Subjects.Persistence;

internal sealed class SubjectsDbContext(DbContextOptions<SubjectsDbContext> options) : DbContext(options)
{
    public DbSet<Subject> Subjects => Set<Subject>();

    public DbSet<TeacherSubject> TeacherSubjects => Set<TeacherSubject>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(SubjectsDbContext).Assembly);
    }
}
