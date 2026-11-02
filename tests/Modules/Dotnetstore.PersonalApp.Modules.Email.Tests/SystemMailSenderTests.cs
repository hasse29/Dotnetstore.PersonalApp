using Dotnetstore.PersonalApp.Modules.Email.Features.Messages;
using Dotnetstore.PersonalApp.Shared.Auth;
using Dotnetstore.PersonalApp.Shared.Email;

namespace Dotnetstore.PersonalApp.Modules.Email.Tests;

public sealed class SystemMailSenderTests : IDisposable
{
    private readonly EmailFixture _fixture = new();
    private readonly Guid _user = Guid.NewGuid();

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public void Dispose() => _fixture.Dispose();

    private SystemMailSender Sender(bool signedIn = true) =>
        new(_fixture.DbFactory, _fixture.Send, new FakeCurrentUser(signedIn ? new AuthenticatedUser(_user, "anna", "Anna Andersson") : null));

    private static SystemMail Mail() =>
        new("bo@example.com", "Time reports", "Hello", [new MailFile("report.xlsx", new byte[] { 1, 2, 3 })]);

    [Fact]
    public async Task SendsThroughTheSystemSmtpAccountWithTheAttachment()
    {
        await _fixture.Create.HandleAsync(EmailFixture.ValidAccount(_user, "Personal"), Ct);
        await _fixture.Create.HandleAsync(EmailFixture.ValidAccount(_user, "System out", isSystem: true) with { EmailAddress = "system@example.com" }, Ct);

        var result = await Sender().SendAsync(Mail(), Ct);

        Assert.True(result.IsSuccess);
        var sent = Assert.Single(_fixture.Gateway.Sent);
        Assert.Equal("system@example.com", sent.From);
        Assert.Equal(["bo@example.com"], sent.To);
        var attachment = Assert.Single(sent.Attachments);
        Assert.Equal("report.xlsx", attachment.FileName);
        Assert.Equal(new byte[] { 1, 2, 3 }, attachment.Content);
    }

    [Fact]
    public async Task FailsWhenThereIsNoSystemSmtpAccountEvenIfTheUserHasOwnOnes()
    {
        await _fixture.Create.HandleAsync(EmailFixture.ValidAccount(_user), Ct);
        await _fixture.Create.HandleAsync(EmailFixture.ValidPop3Account(_user, isSystem: true), Ct);

        var result = await Sender().SendAsync(Mail(), Ct);

        Assert.Equal("email_no_system_smtp", result.Error.Code);
        Assert.Empty(_fixture.Gateway.Sent);
    }

    [Fact]
    public async Task FailsWhenNobodyIsSignedIn()
    {
        await _fixture.Create.HandleAsync(EmailFixture.ValidAccount(_user, isSystem: true), Ct);

        var result = await Sender(signedIn: false).SendAsync(Mail(), Ct);

        Assert.False(result.IsSuccess);
        Assert.Empty(_fixture.Gateway.Sent);
    }

    private sealed class FakeCurrentUser(AuthenticatedUser? user) : ICurrentUser
    {
        public AuthenticatedUser? User { get; } = user;
    }
}
