using Dotnetstore.PersonalApp.Modules.Email.Domain;
using Dotnetstore.PersonalApp.Modules.Email.Features.EmailAccounts;
using Dotnetstore.PersonalApp.Modules.Email.Features.Messages;
using Dotnetstore.PersonalApp.Modules.Email.Mail;

namespace Dotnetstore.PersonalApp.Modules.Email.Tests;

public sealed class EmailMessageHandlerTests : IDisposable
{
    private readonly EmailFixture _fixture = new();
    private readonly Guid _user = Guid.NewGuid();

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public void Dispose() => _fixture.Dispose();

    private static FetchedMessage Message(string uid, int day) =>
        new(uid, "bo@example.com", "Hello " + uid, new DateTimeOffset(2026, 10, day, 8, 0, 0, TimeSpan.Zero), "Body " + uid, []);

    [Fact]
    public async Task TestConnectionUsesTheTypedSettingsAndReportsSuccess()
    {
        var command = new TestEmailConnection.Command(_user, null, MailProtocol.Smtp, " smtp.example.com ", 587, ConnectionSecurity.StartTls, "anna", "typed");

        var result = await _fixture.Test.HandleAsync(command, Ct);

        Assert.True(result.IsSuccess);
        var server = Assert.Single(_fixture.Gateway.Connections);
        Assert.Equal("smtp.example.com", server.Host);
        Assert.Equal("typed", server.Password);
    }

    [Fact]
    public async Task TestConnectionFallsBackToTheStoredPasswordWhenEditing()
    {
        var account = (await _fixture.Create.HandleAsync(EmailFixture.ValidAccount(_user), Ct)).Value;

        await _fixture.Test.HandleAsync(
            new TestEmailConnection.Command(_user, account.Id, MailProtocol.Smtp, "smtp.example.com", 587, ConnectionSecurity.StartTls, "anna", null),
            Ct);

        Assert.Equal("s3cret", Assert.Single(_fixture.Gateway.Connections).Password);
    }

    [Fact]
    public async Task TestConnectionDoesNotRevealAnotherUsersStoredPassword()
    {
        var foreign = (await _fixture.Create.HandleAsync(EmailFixture.ValidAccount(Guid.NewGuid()), Ct)).Value;

        var result = await _fixture.Test.HandleAsync(
            new TestEmailConnection.Command(_user, foreign.Id, MailProtocol.Smtp, "smtp.example.com", 587, ConnectionSecurity.StartTls, "anna", null),
            Ct);

        Assert.Equal("email_account_not_found", result.Error.Code);
        Assert.Empty(_fixture.Gateway.Connections);
    }

    [Fact]
    public async Task TestConnectionReportsTheGatewayFailureAndValidatesFirst()
    {
        _fixture.Gateway.Failure = new MailGatewayException("email_authentication_failed", "535 bad login");
        var failed = await _fixture.Test.HandleAsync(
            new TestEmailConnection.Command(_user, null, MailProtocol.Smtp, "smtp.example.com", 587, ConnectionSecurity.StartTls, "anna", "x"),
            Ct);
        var invalid = await _fixture.Test.HandleAsync(
            new TestEmailConnection.Command(_user, null, MailProtocol.Smtp, "smtp.example.com", 0, ConnectionSecurity.StartTls, "anna", "x"),
            Ct);

        Assert.Equal("email_authentication_failed", failed.Error.Code);
        Assert.Equal("535 bad login", failed.Error.Message);
        Assert.Equal("email_account_port_invalid", invalid.Error.Code);
        Assert.Single(_fixture.Gateway.Connections);
    }

    [Fact]
    public async Task SendSendsFromTheAccountAddressToEveryRecipient()
    {
        var account = (await _fixture.Create.HandleAsync(EmailFixture.ValidAccount(_user), Ct)).Value;

        var result = await _fixture.Send.HandleAsync(
            new SendEmail.Command(_user, account.Id, "bo@example.com; cy@example.com,BO@example.com", "Hi", "Text"),
            Ct);

        Assert.Equal(2, result.Value);
        var sent = Assert.Single(_fixture.Gateway.Sent);
        Assert.Equal("anna@example.com", sent.From);
        Assert.Equal(["bo@example.com", "cy@example.com"], sent.To);
        Assert.Equal("Hi", sent.Subject);
        Assert.Equal("s3cret", Assert.Single(_fixture.Gateway.Connections).Password);
    }

    [Fact]
    public async Task SendCanUseASystemAccountButNotAnotherUsersOrAPop3Account()
    {
        var system = (await _fixture.Create.HandleAsync(EmailFixture.ValidAccount(_user, isSystem: true), Ct)).Value;
        var foreign = (await _fixture.Create.HandleAsync(EmailFixture.ValidAccount(Guid.NewGuid(), "Theirs"), Ct)).Value;
        var pop3 = (await _fixture.Create.HandleAsync(EmailFixture.ValidPop3Account(_user), Ct)).Value;
        var otherUser = Guid.NewGuid();

        var viaSystem = await _fixture.Send.HandleAsync(new SendEmail.Command(otherUser, system.Id, "bo@example.com", "Hi", "x"), Ct);
        var viaForeign = await _fixture.Send.HandleAsync(new SendEmail.Command(_user, foreign.Id, "bo@example.com", "Hi", "x"), Ct);
        var viaPop3 = await _fixture.Send.HandleAsync(new SendEmail.Command(_user, pop3.Id, "bo@example.com", "Hi", "x"), Ct);

        Assert.True(viaSystem.IsSuccess);
        Assert.Equal("email_account_not_found", viaForeign.Error.Code);
        Assert.Equal("email_account_not_smtp", viaPop3.Error.Code);
        Assert.Single(_fixture.Gateway.Sent);
    }

    [Theory]
    [InlineData("", "email_recipient_required")]
    [InlineData(" , ; ", "email_recipient_required")]
    [InlineData("bo@example.com, nonsense", "email_recipient_invalid")]
    public async Task SendRejectsMissingOrInvalidRecipients(string to, string expectedCode)
    {
        var account = (await _fixture.Create.HandleAsync(EmailFixture.ValidAccount(_user), Ct)).Value;

        var result = await _fixture.Send.HandleAsync(new SendEmail.Command(_user, account.Id, to, "Hi", "x"), Ct);

        Assert.Equal(expectedCode, result.Error.Code);
        Assert.Empty(_fixture.Gateway.Sent);
    }

    [Fact]
    public async Task SendRejectsTooManyRecipientsAndTooLongText()
    {
        var account = (await _fixture.Create.HandleAsync(EmailFixture.ValidAccount(_user), Ct)).Value;
        var many = string.Join(',', Enumerable.Range(0, 51).Select(i => $"u{i}@example.com"));

        var tooMany = await _fixture.Send.HandleAsync(new SendEmail.Command(_user, account.Id, many, "Hi", "x"), Ct);
        var longSubject = await _fixture.Send.HandleAsync(new SendEmail.Command(_user, account.Id, "bo@example.com", new string('a', 501), "x"), Ct);
        var longBody = await _fixture.Send.HandleAsync(new SendEmail.Command(_user, account.Id, "bo@example.com", "Hi", new string('a', 100_001)), Ct);

        Assert.Equal("email_too_many_recipients", tooMany.Error.Code);
        Assert.Equal("email_subject_too_long", longSubject.Error.Code);
        Assert.Equal("email_body_too_long", longBody.Error.Code);
    }

    [Fact]
    public async Task SendReportsTheGatewayFailure()
    {
        var account = (await _fixture.Create.HandleAsync(EmailFixture.ValidAccount(_user), Ct)).Value;
        _fixture.Gateway.Failure = new MailGatewayException("email_connection_failed", "timed out");

        var result = await _fixture.Send.HandleAsync(new SendEmail.Command(_user, account.Id, "bo@example.com", "Hi", "x"), Ct);

        Assert.Equal("email_connection_failed", result.Error.Code);
    }

    [Fact]
    public async Task FetchStoresNewMessagesOnceAndListsThemNewestFirst()
    {
        var account = (await _fixture.Create.HandleAsync(EmailFixture.ValidPop3Account(_user), Ct)).Value;
        _fixture.Gateway.Mailbox.AddRange([Message("a", 1), Message("b", 3), Message("c", 2)]);

        var first = await _fixture.Fetch.HandleAsync(new FetchEmail.Command(_user, account.Id), Ct);
        _fixture.Gateway.Mailbox.Add(Message("d", 4));
        var second = await _fixture.Fetch.HandleAsync(new FetchEmail.Command(_user, account.Id), Ct);
        var third = await _fixture.Fetch.HandleAsync(new FetchEmail.Command(_user, account.Id), Ct);
        var list = await _fixture.ListMessages.HandleAsync(new ListEmailMessages.Query(_user, account.Id), Ct);

        Assert.Equal(3, first.Value);
        Assert.Equal(1, second.Value);
        Assert.Equal(0, third.Value);
        Assert.Equal(4, list.Value.TotalCount);
        Assert.Equal(["d", "b", "c", "a"], list.Value.Items.Select(m => m.Subject["Hello ".Length..]));
        Assert.All(list.Value.Items, m => Assert.Equal(string.Empty, m.Body));
    }

    [Fact]
    public async Task FetchTakesAtMostTheLimitPerCall()
    {
        var account = (await _fixture.Create.HandleAsync(EmailFixture.ValidPop3Account(_user), Ct)).Value;
        _fixture.Gateway.Mailbox.AddRange(Enumerable.Range(1, 60).Select(i => new FetchedMessage($"u{i}", "bo@example.com", "S", DateTimeOffset.UnixEpoch.AddDays(i), "B", [])));

        var first = await _fixture.Fetch.HandleAsync(new FetchEmail.Command(_user, account.Id), Ct);
        var second = await _fixture.Fetch.HandleAsync(new FetchEmail.Command(_user, account.Id), Ct);

        Assert.Equal(FetchEmail.MaxPerFetch, first.Value);
        Assert.Equal(10, second.Value);
        Assert.Equal(60, await _fixture.MessageCountAsync());
    }

    [Fact]
    public async Task FetchRejectsSmtpAccountsOtherUsersAccountsAndReportsGatewayFailures()
    {
        var smtp = (await _fixture.Create.HandleAsync(EmailFixture.ValidAccount(_user), Ct)).Value;
        var foreign = (await _fixture.Create.HandleAsync(EmailFixture.ValidPop3Account(Guid.NewGuid(), "Theirs"), Ct)).Value;
        var pop3 = (await _fixture.Create.HandleAsync(EmailFixture.ValidPop3Account(_user), Ct)).Value;
        _fixture.Gateway.Failure = new MailGatewayException("email_tls_failed", "bad cert");

        var viaSmtp = await _fixture.Fetch.HandleAsync(new FetchEmail.Command(_user, smtp.Id), Ct);
        var viaForeign = await _fixture.Fetch.HandleAsync(new FetchEmail.Command(_user, foreign.Id), Ct);
        var failed = await _fixture.Fetch.HandleAsync(new FetchEmail.Command(_user, pop3.Id), Ct);

        Assert.Equal("email_account_not_pop3", viaSmtp.Error.Code);
        Assert.Equal("email_account_not_found", viaForeign.Error.Code);
        Assert.Equal("email_tls_failed", failed.Error.Code);
    }

    [Fact]
    public async Task SystemInboxIsSharedButAPersonalInboxIsNot()
    {
        var system = (await _fixture.Create.HandleAsync(EmailFixture.ValidPop3Account(_user, "Info", isSystem: true), Ct)).Value;
        var personal = (await _fixture.Create.HandleAsync(EmailFixture.ValidPop3Account(_user), Ct)).Value;
        _fixture.Gateway.Mailbox.Add(Message("a", 1));
        await _fixture.Fetch.HandleAsync(new FetchEmail.Command(_user, system.Id), Ct);
        await _fixture.Fetch.HandleAsync(new FetchEmail.Command(_user, personal.Id), Ct);
        var someoneElse = Guid.NewGuid();

        var systemList = await _fixture.ListMessages.HandleAsync(new ListEmailMessages.Query(someoneElse, system.Id), Ct);
        var personalList = await _fixture.ListMessages.HandleAsync(new ListEmailMessages.Query(someoneElse, personal.Id), Ct);

        Assert.Equal(1, systemList.Value.TotalCount);
        Assert.Equal("email_account_not_found", personalList.Error.Code);
    }

    [Fact]
    public async Task GetReturnsTheFullMessageOnlyToThoseWhoMayUseTheAccount()
    {
        var account = (await _fixture.Create.HandleAsync(EmailFixture.ValidPop3Account(_user), Ct)).Value;
        _fixture.Gateway.Mailbox.Add(Message("a", 1));
        await _fixture.Fetch.HandleAsync(new FetchEmail.Command(_user, account.Id), Ct);
        var summary = (await _fixture.ListMessages.HandleAsync(new ListEmailMessages.Query(_user, account.Id), Ct)).Value.Items[0];

        var own = await _fixture.GetMessage.HandleAsync(new GetEmailMessage.Query(summary.Id, _user), Ct);
        var foreign = await _fixture.GetMessage.HandleAsync(new GetEmailMessage.Query(summary.Id, Guid.NewGuid()), Ct);
        var missing = await _fixture.GetMessage.HandleAsync(new GetEmailMessage.Query(Guid.NewGuid(), _user), Ct);

        Assert.Equal("Body a", own.Value.Body);
        Assert.Equal("email_message_not_found", foreign.Error.Code);
        Assert.Equal("email_message_not_found", missing.Error.Code);
    }

    [Fact]
    public async Task DeletingAnAccountDeletesItsMessages()
    {
        var account = (await _fixture.Create.HandleAsync(EmailFixture.ValidPop3Account(_user), Ct)).Value;
        _fixture.Gateway.Mailbox.AddRange([Message("a", 1), Message("b", 2)]);
        await _fixture.Fetch.HandleAsync(new FetchEmail.Command(_user, account.Id), Ct);

        await _fixture.Delete.HandleAsync(new DeleteEmailAccount.Command(account.Id, _user), Ct);

        Assert.Equal(0, await _fixture.MessageCountAsync());
    }

    [Fact]
    public async Task DeletingAUsersAccountsDeletesTheirMessagesButNotTheSystemInbox()
    {
        var system = (await _fixture.Create.HandleAsync(EmailFixture.ValidPop3Account(_user, "Info", isSystem: true), Ct)).Value;
        var personal = (await _fixture.Create.HandleAsync(EmailFixture.ValidPop3Account(_user), Ct)).Value;
        _fixture.Gateway.Mailbox.Add(Message("a", 1));
        await _fixture.Fetch.HandleAsync(new FetchEmail.Command(_user, system.Id), Ct);
        await _fixture.Fetch.HandleAsync(new FetchEmail.Command(_user, personal.Id), Ct);

        await _fixture.DeleteForUser.HandleAsync(new DeleteEmailAccountsForUser.Command(_user), Ct);

        Assert.Equal(1, await _fixture.MessageCountAsync());
    }
}
