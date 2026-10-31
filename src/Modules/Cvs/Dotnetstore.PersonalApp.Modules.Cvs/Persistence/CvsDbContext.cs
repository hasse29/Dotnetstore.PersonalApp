using Dotnetstore.PersonalApp.Modules.Cvs.Domain;
using Microsoft.EntityFrameworkCore;

namespace Dotnetstore.PersonalApp.Modules.Cvs.Persistence;

internal sealed class CvsDbContext(DbContextOptions<CvsDbContext> options) : DbContext(options)
{
    public DbSet<Cv> Cvs => Set<Cv>();

    public DbSet<WorkExperience> WorkExperiences => Set<WorkExperience>();

    public DbSet<Education> Educations => Set<Education>();

    public DbSet<Course> Courses => Set<Course>();

    public DbSet<LanguageSkill> LanguageSkills => Set<LanguageSkill>();

    public DbSet<CvContactSelection> ContactSelections => Set<CvContactSelection>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CvsDbContext).Assembly);
    }
}
