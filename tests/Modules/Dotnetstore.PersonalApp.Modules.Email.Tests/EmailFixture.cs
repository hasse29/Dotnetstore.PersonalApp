using Dotnetstore.PersonalApp.Modules.Email.Domain;
using Dotnetstore.PersonalApp.Modules.Email.Features.EmailAccounts;
using Dotnetstore.PersonalApp.Modules.Email.Features.Messages;
using Dotnetstore.PersonalApp.Modules.Email.Mail;
using Dotnetstore.PersonalApp.Modules.Email.Persistence;
using Dotnetstore.PersonalApp.Modules.Email.Resources;
using Dotnetstore.PersonalApp.Modules.Email.Security;
using Dotnetstore.PersonalApp.Shared.Files;
using Dotnetstore.PersonalApp.Shared.Localization;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Dotnetstore.PersonalApp.Modules.Email.Tests;

/// <summary>Real SQLite (in-memory, kept alive by one open connection) with the module's handlers wired up.</summary>
internal sealed class EmailFixture : IDisposable
{
    private readonly SqliteConnection _connection = new("Data Source=:memory:");

    public EmailFixture()
    {
        _connection.Open();
        DbFactory = new TestDbFactory(new DbContextOptionsBuilder<EmailDbContext>().UseSqlite(_connection).Options);
        using var db = DbFactory.CreateDbContext();
        db.Database.EnsureCreated();

        Localization.AddResources(EmailStrings.ResourceManager);
        Localization.SetCulture("en");

        Create = new CreateEmailAccount.Handler(DbFactory, new CreateEmailAccount.Validator(), Protector, Clock);
        Get = new GetEmailAccount.Handler(DbFactory);
        List = new ListEmailAccounts.Handler(DbFactory);
        Update = new UpdateEmailAccount.Handler(DbFactory, new UpdateEmailAccount.Validator(), Protector, Clock);
        Delete = new DeleteEmailAccount.Handler(DbFactory);
        DeleteForUser = new DeleteEmailAccountsForUser.Handler(DbFactory);
        Test = new TestEmailConnection.Handler(DbFactory, new TestEmailConnection.Validator(), Protector, Gateway);
        Send = new SendEmail.Handler(DbFactory, new SendEmail.Validator(), Protector, Gateway, Clock);
        Fetch = new FetchEmail.Handler(DbFactory, Protector, Gateway, Clock);
        ListMessages = new ListEmailMessages.Handler(DbFactory);
        GetMessage = new GetEmailMessage.Handler(DbFactory);
        DeleteMessage = new DeleteEmailMessage.Handler(DbFactory);
        GetAttachment = new GetEmailAttachment.Handler(DbFactory);
        ListSent = new ListSentMessages.Handler(DbFactory);
        GetSent = new GetSentMessage.Handler(DbFactory);
        DeleteSent = new DeleteSentMessage.Handler(DbFactory);
        SaveDraft = new SaveDraft.Handler(DbFactory, new SaveDraft.Validator(), Clock);
        ListDrafts = new ListDrafts.Handler(DbFactory);
        GetDraft = new GetDraft.Handler(DbFactory);
        DeleteDraft = new DeleteDraft.Handler(DbFactory);
    }

    public TimeProvider Clock { get; } = new FixedClock(new DateTimeOffset(2026, 11, 1, 12, 0, 0, TimeSpan.Zero));

    public Localizer Localization { get; } = new();

    public ReversingProtector Protector { get; } = new();

    public FakeMailGateway Gateway { get; } = new();

    public IDbContextFactory<EmailDbContext> DbFactory { get; }

    public CreateEmailAccount.Handler Create { get; }

    public GetEmailAccount.Handler Get { get; }

    public ListEmailAccounts.Handler List { get; }

    public UpdateEmailAccount.Handler Update { get; }

    public DeleteEmailAccount.Handler Delete { get; }

    public DeleteEmailAccountsForUser.Handler DeleteForUser { get; }

    public TestEmailConnection.Handler Test { get; }

    public SendEmail.Handler Send { get; }

    public FetchEmail.Handler Fetch { get; }

    public ListEmailMessages.Handler ListMessages { get; }

    public GetEmailMessage.Handler GetMessage { get; }

    public DeleteEmailMessage.Handler DeleteMessage { get; }

    public GetEmailAttachment.Handler GetAttachment { get; }

    public ListSentMessages.Handler ListSent { get; }

    public GetSentMessage.Handler GetSent { get; }

    public DeleteSentMessage.Handler DeleteSent { get; }

    public SaveDraft.Handler SaveDraft { get; }

    public ListDrafts.Handler ListDrafts { get; }

    public GetDraft.Handler GetDraft { get; }

    public DeleteDraft.Handler DeleteDraft { get; }

    public FakeFileSaveService FileSave { get; } = new();

    public FakeFilePickService FilePick { get; } = new();

    public static CreateEmailAccount.Command ValidAccount(Guid userId, string name = "Work outgoing", bool isSystem = false) =>
        new(userId, isSystem, name, MailProtocol.Smtp, "smtp.example.com", 587, ConnectionSecurity.StartTls, "anna@example.com", "anna", "s3cret");

    public static CreateEmailAccount.Command ValidPop3Account(Guid userId, string name = "Work incoming", bool isSystem = false) =>
        new(userId, isSystem, name, MailProtocol.Pop3, "pop.example.com", 995, ConnectionSecurity.SslOnConnect, "anna@example.com", "anna", "s3cret");

    public async Task<int> MessageCountAsync()
    {
        await using var db = await DbFactory.CreateDbContextAsync();
        return await db.Messages.CountAsync();
    }

    internal sealed class FakeFileSaveService : IFileSaveService
    {
        public string? SavedName { get; private set; }

        public byte[]? SavedContent { get; private set; }

        public Task<string?> SaveAsync(string title, string suggestedFileName, byte[] content, CancellationToken cancellationToken)
        {
            SavedName = suggestedFileName;
            SavedContent = content;
            return Task.FromResult<string?>(suggestedFileName);
        }
    }

    internal sealed class FakeFilePickService : IFilePickService
    {
        public List<PickedFile> Next { get; } = [];

        public List<string> Skipped { get; } = [];

        public Task<FilePickResult> PickFilesAsync(string title, long maxBytes, CancellationToken cancellationToken) =>
            Task.FromResult(new FilePickResult([.. Next], [.. Skipped]));
    }

    /// <summary>Stands in for the mail servers: records what it was asked and answers from a prepared mailbox, or fails on request.</summary>
    internal sealed class FakeMailGateway : IMailGateway
    {
        public MailGatewayException? Failure { get; set; }

        public List<FetchedMessage> Mailbox { get; } = [];

        public List<MailServer> Connections { get; } = [];

        public List<OutgoingMessage> Sent { get; } = [];

        public Task TestAsync(MailServer server, CancellationToken cancellationToken)
        {
            Connections.Add(server);
            return Failure is null ? Task.CompletedTask : Task.FromException(Failure);
        }

        public Task SendAsync(MailServer server, OutgoingMessage message, CancellationToken cancellationToken)
        {
            Connections.Add(server);
            if (Failure is not null)
            {
                return Task.FromException(Failure);
            }

            Sent.Add(message);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<FetchedMessage>> FetchAsync(
            MailServer server,
            IReadOnlySet<string> knownUids,
            int maxMessages,
            CancellationToken cancellationToken)
        {
            Connections.Add(server);
            if (Failure is not null)
            {
                return Task.FromException<IReadOnlyList<FetchedMessage>>(Failure);
            }

            IReadOnlyList<FetchedMessage> result = [.. Mailbox.Where(m => !knownUids.Contains(m.Uid)).OrderByDescending(m => m.SentAt).Take(maxMessages)];
            return Task.FromResult(result);
        }
    }

    public async Task<string?> StoredPasswordAsync(Guid accountId)
    {
        await using var db = await DbFactory.CreateDbContextAsync();
        return await db.Accounts.Where(a => a.Id == accountId).Select(a => a.ProtectedPassword).SingleAsync();
    }

    public void Dispose() => _connection.Dispose();

    /// <summary>Stands in for DPAPI, which would tie the tests to the Windows account: "protects" by wrapping the text.</summary>
    internal sealed class ReversingProtector : ISecretProtector
    {
        public string Protect(string plainText) => "protected:" + plainText;

        public string Unprotect(string protectedText) => protectedText["protected:".Length..];
    }

    private sealed class TestDbFactory(DbContextOptions<EmailDbContext> options) : IDbContextFactory<EmailDbContext>
    {
        public EmailDbContext CreateDbContext() => new(options);
    }

    private sealed class FixedClock(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
