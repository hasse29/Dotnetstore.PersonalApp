using Dotnetstore.PersonalApp.Modules.TimeReports.Domain;
using Microsoft.EntityFrameworkCore;

namespace Dotnetstore.PersonalApp.Modules.TimeReports.Persistence;

internal sealed class TimeReportsDbContext(DbContextOptions<TimeReportsDbContext> options) : DbContext(options)
{
    public DbSet<WorkTimeType> WorkTimeTypes => Set<WorkTimeType>();

    public DbSet<TimeReport> TimeReports => Set<TimeReport>();

    public DbSet<Expense> Expenses => Set<Expense>();

    public DbSet<ExpenseReceipt> ExpenseReceipts => Set<ExpenseReceipt>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(TimeReportsDbContext).Assembly);
    }
}