using System.Security.Cryptography;
using Dotnetstore.PersonalApp.Modules.Email.Domain;
using Dotnetstore.PersonalApp.Modules.Email.Features.EmailAccounts;
using Dotnetstore.PersonalApp.Modules.Email.Features.Messages;
using Dotnetstore.PersonalApp.Modules.Email.Security;
using Microsoft.EntityFrameworkCore;

namespace Dotnetstore.PersonalApp.Modules.Email.Tests;

public sealed class AccessAttachmentAndSecretTests : IDisposable
{
    private readonly EmailFixture _fixture = new();

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public void Dispose() => _fixture.Dispose();

    // A system account is still created by a user; the handler stores it under the shared system owner.
    private async Task<Guid> CreateAccountAsync(Guid owner, bool system = false) =>
        (await _fixture.Create.HandleAsync(EmailFixture.ValidAccount(owner, isSystem: system), Ct)).Value.Id;

    [Fact]
    public async Task AUserFindsTheirOwnAccountAndTheSystemAccountsButNotSomeoneElses()
    {
        var anna = Guid.NewGuid();
        var own = await CreateAccountAsync(anna);
        var system = await CreateAccountAsync(Guid.NewGuid(), system: true);
        var other = await CreateAccountAsync(Guid.NewGuid());
        await using var db = await _fixture.DbFactory.CreateDbContextAsync(Ct);

        (await EmailAccountAccess.FindAsync(db, own, anna, Ct)).IsSuccess.ShouldBeTrue();
        (await EmailAccountAccess.FindAsync(db, system, anna, Ct)).IsSuccess.ShouldBeTrue();
        (await EmailAccountAccess.FindAsync(db, other, anna, Ct)).Error.Code.ShouldBe("email_account_not_found");
    }

    [Fact]
    public async Task NobodySignedInFindsNothing()
    {
        var system = await CreateAccountAsync(Guid.NewGuid(), system: true);
        await using var db = await _fixture.DbFactory.CreateDbContextAsync(Ct);

        (await EmailAccountAccess.FindAsync(db, system, Guid.Empty, Ct)).Error.Code.ShouldBe("email_account_user_required");
    }

    [Fact]
    public void TheServerCarriesTheDecryptedPassword()
    {
        var account = new EmailAccount { Host = "smtp.example.com", Port = 587, Username = "anna", ProtectedPassword = _fixture.Protector.Protect("s3cret") };

        var server = EmailAccountAccess.ToServer(account, _fixture.Protector);

        server.IsSuccess.ShouldBeTrue();
        server.Value.Host.ShouldBe("smtp.example.com");
        server.Value.Password.ShouldBe("s3cret");
    }

    [Fact]
    public void AnAccountWithoutAPasswordHasNoPassword()
    {
        var account = new EmailAccount { ProtectedPassword = null };

        EmailAccountAccess.DecryptPassword(account, _fixture.Protector).Value.ShouldBeNull();
    }

    [Fact]
    public void APasswordThatCannotBeDecryptedIsReportedInsteadOfThrown()
    {
        var account = new EmailAccount { ProtectedPassword = "copied from another machine" };

        var result = EmailAccountAccess.DecryptPassword(account, new FailingProtector());

        result.Error.Code.ShouldBe("email_password_unreadable");
    }

    [Fact]
    public async Task ListsAttachmentsOfEachKindSortedByFileNameWithTheirSizeAndNothingElse()
    {
        var received = Guid.NewGuid();
        var sent = Guid.NewGuid();
        var draft = Guid.NewGuid();
        await using (var db = await _fixture.DbFactory.CreateDbContextAsync(Ct))
        {
            // The messages themselves are not needed to test the queries.
            await db.Database.ExecuteSqlRawAsync("PRAGMA foreign_keys = OFF", Ct);
            db.Attachments.AddRange(
                Attachment(received: received, name: "b.txt", size: 3),
                Attachment(received: received, name: "a.pdf", size: 10),
                Attachment(sent: sent, name: "sent.png", size: 5),
                Attachment(draft: draft, name: "draft.doc", size: 7));
            await db.SaveChangesAsync(Ct);
        }

        await using var read = await _fixture.DbFactory.CreateDbContextAsync(Ct);

        var receivedList = await AttachmentQueries.OfReceivedAsync(read, received, Ct);
        receivedList.Select(a => (a.FileName, a.Size)).ShouldBe([("a.pdf", 10), ("b.txt", 3)]);
        (await AttachmentQueries.OfSentAsync(read, sent, Ct)).Single().FileName.ShouldBe("sent.png");
        (await AttachmentQueries.OfDraftAsync(read, draft, Ct)).Single().Size.ShouldBe(7);
        (await AttachmentQueries.OfDraftAsync(read, Guid.NewGuid(), Ct)).ShouldBeEmpty();
    }

    [Fact]
    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    public void WindowsDataProtectionRoundTripsAndNeverStoresThePasswordInPlainText()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        var protector = new DpapiSecretProtector();

        var stored = protector.Protect("s3cret");

        stored.ShouldNotContain("s3cret");
        protector.Unprotect(stored).ShouldBe("s3cret");
    }

    [Fact]
    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    public void WindowsDataProtectionRejectsDataItDidNotProtect()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        var foreign = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));

        Should.Throw<CryptographicException>(() => new DpapiSecretProtector().Unprotect(foreign));
    }

    private static MailAttachment Attachment(Guid? received = null, Guid? sent = null, Guid? draft = null, string name = "f", int size = 1) => new()
    {
        Id = Guid.NewGuid(),
        ReceivedMessageId = received,
        SentMessageId = sent,
        DraftId = draft,
        FileName = name,
        ContentType = "application/octet-stream",
        Content = new byte[size],
    };

    private sealed class FailingProtector : ISecretProtector
    {
        public string Protect(string plainText) => plainText;

        public string Unprotect(string protectedText) => throw new CryptographicException("wrong user");
    }
}
