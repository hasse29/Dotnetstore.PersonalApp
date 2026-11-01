using Dotnetstore.PersonalApp.Modules.Email.Features.EmailAccounts;
using Dotnetstore.PersonalApp.Modules.Email.Features.Messages;
using Dotnetstore.PersonalApp.Modules.Email.Mail;
using Dotnetstore.PersonalApp.Modules.Email.Resources;
using Dotnetstore.PersonalApp.Shared.Localization;

namespace Dotnetstore.PersonalApp.Modules.Email.Tests;

public sealed class MailViewModelTests : IDisposable
{
    private readonly EmailFixture _fixture = new();
    private readonly Guid _user = Guid.NewGuid();
    private readonly Localizer _localization = new();

    public MailViewModelTests()
    {
        _localization.AddResources(EmailStrings.ResourceManager);
        _localization.SetCulture("en");
    }

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public void Dispose() => _fixture.Dispose();

    private InboxViewModel CreateInbox() =>
        new(_user, _fixture.List, _fixture.Fetch, _fixture.ListMessages, _fixture.GetMessage, _fixture.DeleteMessage, _fixture.GetAttachment, _fixture.FileSave, () => CreateCompose(embedded: true), _localization);

    private ComposeViewModel CreateCompose(bool embedded = false) => new(_user, embedded, _fixture.List, _fixture.Send, _fixture.SaveDraft, _fixture.DeleteDraft, _fixture.FilePick, _localization);

    private EmailAccountEditorViewModel CreateEditor() =>
        new(null, _user, _fixture.Create, _fixture.Update, _fixture.Test, _localization)
        {
            Name = "Work",
            Host = "smtp.example.com",
            EmailAddress = "anna@example.com",
            Username = "anna",
            Password = "pw",
        };

    [Fact]
    public async Task TestConnectionShowsSuccess()
    {
        var editor = CreateEditor();

        await editor.TestConnectionCommand.ExecuteAsync(null);

        Assert.True(editor.TestSucceeded);
        Assert.Equal("Connected and logged in successfully.", editor.TestMessage);
        Assert.False(editor.IsTesting);
    }

    [Fact]
    public async Task TestConnectionShowsTheTranslatedFailureWithTheServersOwnWords()
    {
        _fixture.Gateway.Failure = new MailGatewayException("email_authentication_failed", "535 bad login");
        var editor = CreateEditor();

        await editor.TestConnectionCommand.ExecuteAsync(null);

        Assert.False(editor.TestSucceeded);
        Assert.Equal("The server rejected the username or password. (535 bad login)", editor.TestMessage);
    }

    [Fact]
    public async Task TestConnectionWithBadInputShowsTheValidationMessageOnly()
    {
        var editor = CreateEditor();
        editor.PortText = "abc";

        await editor.TestConnectionCommand.ExecuteAsync(null);

        Assert.False(editor.TestSucceeded);
        Assert.Equal("Port must be a number between 1 and 65535.", editor.TestMessage);
        Assert.Empty(_fixture.Gateway.Connections);
    }

    [Fact]
    public async Task InboxOffersOnlyPop3AccountsAndFetchesIntoTheList()
    {
        await _fixture.Create.HandleAsync(EmailFixture.ValidAccount(_user), Ct);
        await _fixture.Create.HandleAsync(EmailFixture.ValidPop3Account(_user), Ct);
        _fixture.Gateway.Mailbox.Add(new FetchedMessage("a", "bo@example.com", "Hello", DateTimeOffset.UnixEpoch, "Body", []));
        var inbox = CreateInbox();

        await inbox.LoadCommand.ExecuteAsync(null);
        await inbox.FetchCommand.ExecuteAsync(null);

        Assert.Single(inbox.Accounts);
        Assert.Equal("Hello", Assert.Single(inbox.Messages).Subject);
        Assert.Equal("Fetched 1 new message(s).", inbox.StatusMessage);

        inbox.SelectedMessage = inbox.Messages[0];
        await Task.Delay(100, Ct);
        Assert.Equal("Body", inbox.OpenMessage?.Body);
    }

    [Fact]
    public async Task InboxWithoutPop3AccountsSaysSo()
    {
        await _fixture.Create.HandleAsync(EmailFixture.ValidAccount(_user), Ct);
        var inbox = CreateInbox();

        await inbox.LoadCommand.ExecuteAsync(null);

        Assert.True(inbox.HasNoAccounts);
        Assert.False(inbox.FetchCommand.CanExecute(null));
    }

    [Fact]
    public async Task InboxShowsAFetchFailureAsAnError()
    {
        await _fixture.Create.HandleAsync(EmailFixture.ValidPop3Account(_user), Ct);
        _fixture.Gateway.Failure = new MailGatewayException("email_connection_failed", "timed out");
        var inbox = CreateInbox();
        await inbox.LoadCommand.ExecuteAsync(null);

        await inbox.FetchCommand.ExecuteAsync(null);

        Assert.True(inbox.StatusIsError);
        Assert.StartsWith("Could not connect to the server.", inbox.StatusMessage, StringComparison.Ordinal);
        Assert.False(inbox.IsFetching);
    }

    [Fact]
    public async Task ComposeOffersOnlySmtpAccountsSendsAndClearsTheForm()
    {
        await _fixture.Create.HandleAsync(EmailFixture.ValidAccount(_user), Ct);
        await _fixture.Create.HandleAsync(EmailFixture.ValidPop3Account(_user), Ct);
        var compose = CreateCompose();
        await compose.LoadCommand.ExecuteAsync(null);
        compose.To = "bo@example.com";
        compose.Subject = "Hi";
        compose.Body = "Text";

        await compose.SendCommand.ExecuteAsync(null);

        Assert.Single(compose.Accounts);
        Assert.Equal("Message sent to 1 recipient(s).", compose.StatusMessage);
        Assert.False(compose.StatusIsError);
        Assert.Equal(string.Empty, compose.To);
        Assert.Equal("Hi", Assert.Single(_fixture.Gateway.Sent).Subject);
    }

    [Fact]
    public async Task ComposeKeepsTheFormAndShowsTheErrorWhenSendingFails()
    {
        await _fixture.Create.HandleAsync(EmailFixture.ValidAccount(_user), Ct);
        var compose = CreateCompose();
        await compose.LoadCommand.ExecuteAsync(null);
        compose.To = "not an address";
        compose.Subject = "Hi";

        await compose.SendCommand.ExecuteAsync(null);

        Assert.True(compose.StatusIsError);
        Assert.Equal("One of the recipient addresses is not valid.", compose.StatusMessage);
        Assert.Equal("Hi", compose.Subject);
    }
}
