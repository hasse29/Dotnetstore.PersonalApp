using Dotnetstore.PersonalApp.Modules.Email.Domain;
using Dotnetstore.PersonalApp.Modules.Email.Features.EmailAccounts;
using Dotnetstore.PersonalApp.Modules.Email.Features.Messages;
using Dotnetstore.PersonalApp.Modules.Email.Mail;
using Dotnetstore.PersonalApp.Modules.Email.Resources;
using Dotnetstore.PersonalApp.Shared.Localization;

namespace Dotnetstore.PersonalApp.Modules.Email.Tests;

public sealed class ReviewFixTests : IDisposable
{
    private readonly EmailFixture _fixture = new();
    private readonly Guid _user = Guid.NewGuid();
    private readonly Localizer _localization = new();

    public ReviewFixTests()
    {
        _localization.AddResources(EmailStrings.ResourceManager);
        _localization.SetCulture("en");
    }

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public void Dispose() => _fixture.Dispose();

    [Fact]
    public async Task StoredPasswordIsNotUsedToTestAnotherServer()
    {
        var account = (await _fixture.Create.HandleAsync(EmailFixture.ValidAccount(_user), Ct)).Value;

        var elsewhere = await _fixture.Test.HandleAsync(
            new TestEmailConnection.Command(_user, account.Id, MailProtocol.Smtp, "evil.example.net", 587, ConnectionSecurity.StartTls, "anna", null), Ct);
        var otherLogin = await _fixture.Test.HandleAsync(
            new TestEmailConnection.Command(_user, account.Id, MailProtocol.Smtp, "smtp.example.com", 587, ConnectionSecurity.StartTls, "bo", null), Ct);
        var same = await _fixture.Test.HandleAsync(
            new TestEmailConnection.Command(_user, account.Id, MailProtocol.Smtp, "SMTP.example.com", 587, ConnectionSecurity.StartTls, "anna", null), Ct);

        Assert.Equal("email_account_password_required", elsewhere.Error.Code);
        Assert.Equal("email_account_password_required", otherLogin.Error.Code);
        Assert.True(same.IsSuccess);
    }

    [Fact]
    public async Task UpdateKeepsThePasswordOnlyForTheSameServer()
    {
        var account = (await _fixture.Create.HandleAsync(EmailFixture.ValidAccount(_user), Ct)).Value;

        UpdateEmailAccount.Command Change(string host, string? password) =>
            new(account.Id, _user, account.Name, MailProtocol.Smtp, host, 587, ConnectionSecurity.StartTls, "anna@example.com", "anna", password);

        var moved = await _fixture.Update.HandleAsync(Change("evil.example.net", null), Ct);
        var movedWithPassword = await _fixture.Update.HandleAsync(Change("new.example.com", "typed"), Ct);
        var renamed = await _fixture.Update.HandleAsync(Change("new.example.com", null), Ct);

        Assert.Equal("email_account_password_required", moved.Error.Code);
        Assert.True(movedWithPassword.IsSuccess);
        Assert.True(renamed.IsSuccess);
    }

    [Fact]
    public void LongUidsAreHashedNotCut()
    {
        var shortUid = new string('a', 200);
        var first = new string('a', 250) + "1";
        var second = new string('a', 250) + "2";

        Assert.Equal(shortUid, MailLimits.NormalizeUid(shortUid));
        Assert.NotEqual(MailLimits.NormalizeUid(first), MailLimits.NormalizeUid(second));
        Assert.True(MailLimits.NormalizeUid(first).Length <= MailLimits.MaxUidLength);
        Assert.Equal(MailLimits.NormalizeUid(first), MailLimits.NormalizeUid(first));
    }

    [Fact]
    public async Task FetchStoresTwoLongUidsWithTheSameStartAndDoesNotRefetchThem()
    {
        var account = (await _fixture.Create.HandleAsync(EmailFixture.ValidPop3Account(_user), Ct)).Value;
        var when = DateTimeOffset.UnixEpoch;
        _fixture.Gateway.Mailbox.Add(new FetchedMessage(new string('u', 250) + "1", "Bo <bo@example.com>", "One", when, "A", []));
        _fixture.Gateway.Mailbox.Add(new FetchedMessage(new string('u', 250) + "2", "Bo <bo@example.com>", "Two", when.AddMinutes(1), "B", []));

        var first = await _fixture.Fetch.HandleAsync(new FetchEmail.Command(_user, account.Id), Ct);
        var second = await _fixture.Fetch.HandleAsync(new FetchEmail.Command(_user, account.Id), Ct);

        Assert.Equal(2, first.Value);
        Assert.Equal(0, second.Value);
    }

    [Fact]
    public async Task ReSavingADraftKeepsItsAttachmentsTogetherWithItsText()
    {
        var saved = (await _fixture.SaveDraft.HandleAsync(
            new SaveDraft.Command(_user, null, null, "bo@example.com", string.Empty, "S", "one", false, [new AttachmentInput("a.txt", [1])]), Ct)).Value;

        var again = await _fixture.SaveDraft.HandleAsync(
            new SaveDraft.Command(_user, saved.Id, null, "bo@example.com", string.Empty, "S", "two", false, [new AttachmentInput("b.txt", [2])]), Ct);

        Assert.True(again.IsSuccess);
        var full = (await _fixture.GetDraft.HandleAsync(new GetDraft.Query(saved.Id, _user), Ct)).Value;
        Assert.Equal("two", full.Body);
        Assert.Equal("b.txt", Assert.Single(full.Attachments).FileName);
    }

    [Fact]
    public void FormatterSeparatesTableHeaderCellsAndKeepsFormContent()
    {
        var blocks = HtmlFormatter.Format("<form><table><tr><th>Name</th><th>Qty</th></tr></table><p>Inside form</p></form>");

        var text = string.Concat(blocks.SelectMany(b => b.Runs).Select(r => r.Text));
        Assert.Contains("Name  Qty", text, StringComparison.Ordinal);
        Assert.Contains("Inside form", text, StringComparison.Ordinal);
    }

    private InboxViewModel CreateInbox() =>
        new(_user, _fixture.List, _fixture.Fetch, _fixture.ListMessages, _fixture.GetMessage, _fixture.DeleteMessage, _fixture.GetAttachment, _fixture.FileSave, CreateCompose, _localization);

    private ComposeViewModel CreateCompose() =>
        new(_user, true, _fixture.List, _fixture.Send, _fixture.SaveDraft, _fixture.DeleteDraft, _fixture.FilePick, _localization);

    [Fact]
    public async Task RefreshKeepsTheOpenMessageAndItsDeleteConfirmation()
    {
        await _fixture.Create.HandleAsync(EmailFixture.ValidPop3Account(_user), Ct);
        _fixture.Gateway.Mailbox.Add(new FetchedMessage("u1", "Bo <bo@example.com>", "One", DateTimeOffset.UnixEpoch, "A", []));
        var inbox = CreateInbox();
        await inbox.LoadCommand.ExecuteAsync(null);
        await inbox.FetchCommand.ExecuteAsync(null);
        inbox.SelectedMessage = inbox.Messages[0];
        await Task.Delay(150, Ct);
        inbox.RequestDeleteCommand.Execute(null);
        var open = inbox.OpenMessage;
        Assert.NotNull(open);

        _fixture.Gateway.Mailbox.Add(new FetchedMessage("u2", "Cy <cy@example.com>", "Two", DateTimeOffset.UnixEpoch.AddMinutes(5), "B", []));
        await inbox.AutoRefreshCommand.ExecuteAsync(null);
        await Task.Delay(150, Ct);

        Assert.Equal(2, inbox.Messages.Count);
        Assert.Same(open, inbox.OpenMessage);
        Assert.True(inbox.IsConfirmingDelete);
        Assert.Equal(open.Id, inbox.SelectedMessage?.Id);
    }

    [Fact]
    public async Task InboxOffersOlderMessagesBeyondTheFirstPage()
    {
        await _fixture.Create.HandleAsync(EmailFixture.ValidPop3Account(_user), Ct);
        for (var i = 0; i < 60; i++)
        {
            _fixture.Gateway.Mailbox.Add(new FetchedMessage($"u{i}", "Bo <bo@example.com>", $"M{i}", DateTimeOffset.UnixEpoch.AddMinutes(i), "A", []));
        }

        var inbox = CreateInbox();
        await inbox.LoadCommand.ExecuteAsync(null);
        await inbox.FetchCommand.ExecuteAsync(null);
        await inbox.FetchCommand.ExecuteAsync(null);
        await Task.Delay(150, Ct);

        Assert.Equal(ListEmailMessages.MaxPageSize, inbox.Messages.Count);
        Assert.True(inbox.HasMore);

        await inbox.LoadMoreCommand.ExecuteAsync(null);

        Assert.Equal(60, inbox.Messages.Count);
        Assert.False(inbox.HasMore);
    }

    [Fact]
    public async Task ComposerReportsFilesThatWereSkipped()
    {
        await _fixture.Create.HandleAsync(EmailFixture.ValidAccount(_user), Ct);
        var composer = CreateCompose();
        await composer.LoadCommand.ExecuteAsync(null);
        _fixture.FilePick.Skipped.Add("huge.bin");

        await composer.AddAttachmentsCommand.ExecuteAsync(null);

        Assert.True(composer.StatusIsError);
        Assert.Contains("huge.bin", composer.StatusMessage, StringComparison.Ordinal);
    }
}
