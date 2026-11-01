using Dotnetstore.PersonalApp.Modules.Email.Domain;
using Microsoft.EntityFrameworkCore;

namespace Dotnetstore.PersonalApp.Modules.Email.Persistence;

internal sealed class EmailDbContext(DbContextOptions<EmailDbContext> options) : DbContext(options)
{
    public DbSet<EmailAccount> Accounts => Set<EmailAccount>();

    public DbSet<ReceivedMessage> Messages => Set<ReceivedMessage>();

    public DbSet<SentMessage> SentMessages => Set<SentMessage>();

    public DbSet<MailAttachment> Attachments => Set<MailAttachment>();

    public DbSet<DeletedMessage> DeletedMessages => Set<DeletedMessage>();

    public DbSet<Draft> Drafts => Set<Draft>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(EmailDbContext).Assembly);
    }
}
