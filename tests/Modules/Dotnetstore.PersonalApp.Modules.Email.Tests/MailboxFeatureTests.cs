using Dotnetstore.PersonalApp.Modules.Email.Features.Messages;
using Dotnetstore.PersonalApp.Modules.Email.Mail;
using Dotnetstore.PersonalApp.Modules.Email.Resources;
using Dotnetstore.PersonalApp.Shared.Files;
using Dotnetstore.PersonalApp.Shared.Localization;

namespace Dotnetstore.PersonalApp.Modules.Email.Tests;

/// <summary>Sent mail, local deletion, attachments, HTML, reply/forward and the inbox refresh.</summary>
public sealed class MailboxFeatureTests : IDisposable
{
    private readonly EmailFixture _fixture = new();
    private readonly Guid _user = Guid.NewGuid();
    private readonly Localizer _localization = new();

    public MailboxFeatureTests()
    {
        _localization.AddResources(EmailStrings.ResourceManager);
        _localization.SetCulture("en");
    }

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public void Dispose() => _fixture.Dispose();

    private static FetchedMessage Incoming(string uid, params MailAttachmentData[] attachments) =>
        new(uid, "Bo <bo@example.com>", "Hello " + uid, new DateTimeOffset(2026, 10, 1, 8, 0, 0, TimeSpan.Zero), "Line one\nLine two", attachments);

    private static MailAttachmentData Pdf(string name = "a.pdf") => new(name, "application/pdf", [1, 2, 3, 4]);

    private InboxViewModel CreateInbox() =>
        new(_user, _fixture.List, _fixture.Fetch, _fixture.ListMessages, _fixture.GetMessage, _fixture.DeleteMessage, _fixture.GetAttachment, _fixture.FileSave, CreateEmbeddedCompose, _localization);

    private ComposeViewModel CreateEmbeddedCompose() => new(_user, true, _fixture.List, _fixture.Send, _fixture.SaveDraft, _fixture.DeleteDraft, _fixture.FilePick, _localization);

    private SentViewModel CreateSent() =>
        new(_user, _fixture.List, _fixture.ListSent, _fixture.GetSent, _fixture.DeleteSent, _fixture.GetAttachment, _fixture.FileSave, _localization);

    private async Task<InboxViewModel> OpenFirstMessageAsync()
    {
        var inbox = CreateInbox();
        await inbox.LoadCommand.ExecuteAsync(null);
        await inbox.FetchCommand.ExecuteAsync(null);
        inbox.SelectedMessage = inbox.Messages[0];
        await Task.Delay(100, Ct);
        return inbox;
    }

    [Fact]
    public async Task SendKeepsACopyWithHtmlFlagAndAttachments()
    {
        var account = (await _fixture.Create.HandleAsync(EmailFixture.ValidAccount(_user), Ct)).Value;

        var result = await _fixture.Send.HandleAsync(
            new SendEmail.Command(_user, account.Id, "bo@example.com", "Report", "<b>Hi</b>", true, [new AttachmentInput("report.pdf", [9, 9])]),
            Ct);

        Assert.True(result.IsSuccess);
        var sent = Assert.Single(_fixture.Gateway.Sent);
        Assert.True(sent.IsHtml);
        var attachment = Assert.Single(sent.Attachments);
        Assert.Equal("application/pdf", attachment.ContentType);

        var list = (await _fixture.ListSent.HandleAsync(new ListSentMessages.Query(_user, account.Id), Ct)).Value;
        var full = (await _fixture.GetSent.HandleAsync(new GetSentMessage.Query(list.Items[0].Id, _user), Ct)).Value;
        Assert.Equal(string.Empty, list.Items[0].Body);
        Assert.Equal("<b>Hi</b>", full.Body);
        Assert.True(full.IsHtml);
        Assert.Equal("bo@example.com", full.To);
        Assert.Equal(("report.pdf", 2), (Assert.Single(full.Attachments).FileName, Assert.Single(full.Attachments).Size));
    }

    [Fact]
    public async Task AFailedSendLeavesNoCopy()
    {
        var account = (await _fixture.Create.HandleAsync(EmailFixture.ValidAccount(_user), Ct)).Value;
        _fixture.Gateway.Failure = new MailGatewayException("email_connection_failed", "down");

        await _fixture.Send.HandleAsync(new SendEmail.Command(_user, account.Id, "bo@example.com", "Hi", "x"), Ct);

        var list = (await _fixture.ListSent.HandleAsync(new ListSentMessages.Query(_user, account.Id), Ct)).Value;
        Assert.Equal(0, list.TotalCount);
    }

    [Fact]
    public async Task SendRejectsTooManyOrTooLargeAttachments()
    {
        var account = (await _fixture.Create.HandleAsync(EmailFixture.ValidAccount(_user), Ct)).Value;
        var many = Enumerable.Range(0, 21).Select(i => new AttachmentInput($"f{i}.txt", [1])).ToList();
        var big = new[] { new AttachmentInput("big.bin", new byte[MailLimits.MaxAttachmentBytes + 1]) };
        var together = new[]
        {
            new AttachmentInput("a.bin", new byte[MailLimits.MaxAttachmentBytes]),
            new AttachmentInput("b.bin", new byte[MailLimits.MaxAttachmentBytes]),
            new AttachmentInput("c.bin", new byte[MailLimits.MaxAttachmentBytes]),
        };

        var tooMany = await _fixture.Send.HandleAsync(new SendEmail.Command(_user, account.Id, "bo@example.com", "Hi", "x", false, many), Ct);
        var tooBig = await _fixture.Send.HandleAsync(new SendEmail.Command(_user, account.Id, "bo@example.com", "Hi", "x", false, big), Ct);
        var tooBigTogether = await _fixture.Send.HandleAsync(new SendEmail.Command(_user, account.Id, "bo@example.com", "Hi", "x", false, together), Ct);

        Assert.Equal("email_too_many_attachments", tooMany.Error.Code);
        Assert.Equal("email_attachment_too_large", tooBig.Error.Code);
        Assert.Equal("email_attachments_too_large", tooBigTogether.Error.Code);
        Assert.Empty(_fixture.Gateway.Sent);
    }

    [Fact]
    public async Task SentMailFollowsAccountAccessAndCanBeDeleted()
    {
        var mine = (await _fixture.Create.HandleAsync(EmailFixture.ValidAccount(_user), Ct)).Value;
        await _fixture.Send.HandleAsync(new SendEmail.Command(_user, mine.Id, "bo@example.com", "Hi", "x"), Ct);
        var id = (await _fixture.ListSent.HandleAsync(new ListSentMessages.Query(_user, mine.Id), Ct)).Value.Items[0].Id;
        var stranger = Guid.NewGuid();

        var listed = await _fixture.ListSent.HandleAsync(new ListSentMessages.Query(stranger, mine.Id), Ct);
        var got = await _fixture.GetSent.HandleAsync(new GetSentMessage.Query(id, stranger), Ct);
        var deletedByStranger = await _fixture.DeleteSent.HandleAsync(new DeleteSentMessage.Command(id, stranger), Ct);
        var deleted = await _fixture.DeleteSent.HandleAsync(new DeleteSentMessage.Command(id, _user), Ct);

        Assert.Equal("email_account_not_found", listed.Error.Code);
        Assert.Equal("email_message_not_found", got.Error.Code);
        Assert.Equal("email_message_not_found", deletedByStranger.Error.Code);
        Assert.True(deleted.IsSuccess);
        Assert.Equal(0, (await _fixture.ListSent.HandleAsync(new ListSentMessages.Query(_user, mine.Id), Ct)).Value.TotalCount);
    }

    [Fact]
    public async Task FetchStoresAttachmentsAndTheyCanBeReadBackByThoseWithAccess()
    {
        var account = (await _fixture.Create.HandleAsync(EmailFixture.ValidPop3Account(_user), Ct)).Value;
        _fixture.Gateway.Mailbox.Add(Incoming("a", Pdf("b.pdf"), Pdf("a.pdf")));
        await _fixture.Fetch.HandleAsync(new FetchEmail.Command(_user, account.Id), Ct);
        var summary = (await _fixture.ListMessages.HandleAsync(new ListEmailMessages.Query(_user, account.Id), Ct)).Value.Items[0];

        var message = (await _fixture.GetMessage.HandleAsync(new GetEmailMessage.Query(summary.Id, _user), Ct)).Value;
        var content = await _fixture.GetAttachment.HandleAsync(new GetEmailAttachment.Query(message.Attachments[0].Id, _user), Ct);
        var stranger = await _fixture.GetAttachment.HandleAsync(new GetEmailAttachment.Query(message.Attachments[0].Id, Guid.NewGuid()), Ct);
        var missing = await _fixture.GetAttachment.HandleAsync(new GetEmailAttachment.Query(Guid.NewGuid(), _user), Ct);

        Assert.Equal(["a.pdf", "b.pdf"], message.Attachments.Select(a => a.FileName));
        Assert.Equal(4, message.Attachments[0].Size);
        Assert.Equal([1, 2, 3, 4], content.Value.Content);
        Assert.Equal("application/pdf", content.Value.ContentType);
        Assert.Equal("email_attachment_not_found", stranger.Error.Code);
        Assert.Equal("email_attachment_not_found", missing.Error.Code);
    }

    [Fact]
    public async Task FetchSkipsOversizedAttachments()
    {
        var account = (await _fixture.Create.HandleAsync(EmailFixture.ValidPop3Account(_user), Ct)).Value;
        _fixture.Gateway.Mailbox.Add(Incoming("a", Pdf(), new MailAttachmentData("big.bin", "application/octet-stream", new byte[MailLimits.MaxAttachmentBytes + 1])));
        await _fixture.Fetch.HandleAsync(new FetchEmail.Command(_user, account.Id), Ct);
        var summary = (await _fixture.ListMessages.HandleAsync(new ListEmailMessages.Query(_user, account.Id), Ct)).Value.Items[0];

        var message = (await _fixture.GetMessage.HandleAsync(new GetEmailMessage.Query(summary.Id, _user), Ct)).Value;

        Assert.Equal("a.pdf", Assert.Single(message.Attachments).FileName);
    }

    [Fact]
    public async Task DeletedMessagesStayDeletedAfterTheNextFetch()
    {
        var account = (await _fixture.Create.HandleAsync(EmailFixture.ValidPop3Account(_user), Ct)).Value;
        _fixture.Gateway.Mailbox.AddRange([Incoming("a"), Incoming("b")]);
        await _fixture.Fetch.HandleAsync(new FetchEmail.Command(_user, account.Id), Ct);
        var items = (await _fixture.ListMessages.HandleAsync(new ListEmailMessages.Query(_user, account.Id), Ct)).Value.Items;
        var toDelete = items.First(m => m.Subject.EndsWith('a'));

        var deleted = await _fixture.DeleteMessage.HandleAsync(new DeleteEmailMessage.Command(toDelete.Id, _user), Ct);
        var refetched = await _fixture.Fetch.HandleAsync(new FetchEmail.Command(_user, account.Id), Ct);
        var again = await _fixture.DeleteMessage.HandleAsync(new DeleteEmailMessage.Command(toDelete.Id, _user), Ct);

        Assert.True(deleted.IsSuccess);
        Assert.Equal(0, refetched.Value);
        Assert.Equal("email_message_not_found", again.Error.Code);
        Assert.Equal(1, await _fixture.MessageCountAsync());
    }

    [Fact]
    public async Task OnlyThoseWithAccessCanDeleteAMessageAndItsAttachmentsGoWithIt()
    {
        var account = (await _fixture.Create.HandleAsync(EmailFixture.ValidPop3Account(_user), Ct)).Value;
        _fixture.Gateway.Mailbox.Add(Incoming("a", Pdf()));
        await _fixture.Fetch.HandleAsync(new FetchEmail.Command(_user, account.Id), Ct);
        var summary = (await _fixture.ListMessages.HandleAsync(new ListEmailMessages.Query(_user, account.Id), Ct)).Value.Items[0];
        var attachmentId = (await _fixture.GetMessage.HandleAsync(new GetEmailMessage.Query(summary.Id, _user), Ct)).Value.Attachments[0].Id;

        var stranger = await _fixture.DeleteMessage.HandleAsync(new DeleteEmailMessage.Command(summary.Id, Guid.NewGuid()), Ct);
        await _fixture.DeleteMessage.HandleAsync(new DeleteEmailMessage.Command(summary.Id, _user), Ct);
        var attachment = await _fixture.GetAttachment.HandleAsync(new GetEmailAttachment.Query(attachmentId, _user), Ct);

        Assert.Equal("email_message_not_found", stranger.Error.Code);
        Assert.Equal("email_attachment_not_found", attachment.Error.Code);
    }

    [Fact]
    public void MessageRulesPickTheAddressAndAvoidDoublePrefixes()
    {
        Assert.Equal("bo@example.com", MessageRules.ExtractAddress("Bo <bo@example.com>"));
        Assert.Equal("bo@example.com", MessageRules.ExtractAddress("bo@example.com"));
        Assert.Equal("Re: Hi", MessageRules.WithPrefix("Re: ", "Hi"));
        Assert.Equal("RE: Hi", MessageRules.WithPrefix("Re: ", "RE: Hi"));
    }

    [Fact]
    public async Task ReplyOpensAComposerAddressedToTheSenderWithTheOriginalQuoted()
    {
        await _fixture.Create.HandleAsync(EmailFixture.ValidAccount(_user, "Other") with { EmailAddress = "other@example.com" }, Ct);
        await _fixture.Create.HandleAsync(EmailFixture.ValidAccount(_user, "Mine"), Ct);
        await _fixture.Create.HandleAsync(EmailFixture.ValidPop3Account(_user), Ct);
        _fixture.Gateway.Mailbox.Add(Incoming("a"));
        var inbox = await OpenFirstMessageAsync();

        await inbox.ReplyCommand.ExecuteAsync(null);

        var composer = Assert.IsType<ComposeViewModel>(inbox.Composer);
        Assert.Equal("bo@example.com", composer.To);
        Assert.Equal("Re: Hello a", composer.Subject);
        Assert.Contains("> Line one\n> Line two", composer.Body, StringComparison.Ordinal);
        Assert.Contains("Bo <bo@example.com> wrote:", composer.Body, StringComparison.Ordinal);
        Assert.Equal("anna@example.com", composer.SelectedAccount?.EmailAddress);
        Assert.True(composer.IsEmbedded);
    }

    [Fact]
    public async Task ForwardKeepsTheAttachmentsAndSendingClosesTheComposer()
    {
        await _fixture.Create.HandleAsync(EmailFixture.ValidAccount(_user), Ct);
        await _fixture.Create.HandleAsync(EmailFixture.ValidPop3Account(_user), Ct);
        _fixture.Gateway.Mailbox.Add(Incoming("a", Pdf()));
        var inbox = await OpenFirstMessageAsync();

        await inbox.ForwardCommand.ExecuteAsync(null);
        var composer = Assert.IsType<ComposeViewModel>(inbox.Composer);
        composer.To = "cy@example.com";
        await composer.SendCommand.ExecuteAsync(null);

        Assert.Null(inbox.Composer);
        var sent = Assert.Single(_fixture.Gateway.Sent);
        Assert.Equal("Fwd: Hello a", sent.Subject);
        Assert.Contains("Forwarded message", sent.Body, StringComparison.Ordinal);
        Assert.Equal("a.pdf", Assert.Single(sent.Attachments).FileName);
        Assert.Equal("Message sent.", inbox.StatusMessage);
    }

    [Fact]
    public async Task CancellingTheComposerBringsBackTheReadingPane()
    {
        await _fixture.Create.HandleAsync(EmailFixture.ValidAccount(_user), Ct);
        await _fixture.Create.HandleAsync(EmailFixture.ValidPop3Account(_user), Ct);
        _fixture.Gateway.Mailbox.Add(Incoming("a"));
        var inbox = await OpenFirstMessageAsync();
        await inbox.ReplyCommand.ExecuteAsync(null);

        inbox.Composer!.CancelCommand.Execute(null);

        Assert.Null(inbox.Composer);
        Assert.NotNull(inbox.OpenMessage);
    }

    [Fact]
    public async Task DeleteInTheInboxAsksThenRemovesTheMessageLocally()
    {
        await _fixture.Create.HandleAsync(EmailFixture.ValidPop3Account(_user), Ct);
        _fixture.Gateway.Mailbox.AddRange([Incoming("a"), Incoming("b")]);
        var inbox = await OpenFirstMessageAsync();

        inbox.RequestDeleteCommand.Execute(null);
        Assert.True(inbox.IsConfirmingDelete);
        await inbox.ConfirmDeleteCommand.ExecuteAsync(null);

        Assert.Single(inbox.Messages);
        Assert.Null(inbox.OpenMessage);
        Assert.False(inbox.IsConfirmingDelete);
        Assert.Equal(1, await _fixture.MessageCountAsync());
    }

    [Fact]
    public async Task SavingAnAttachmentHandsItToTheSaveDialog()
    {
        await _fixture.Create.HandleAsync(EmailFixture.ValidPop3Account(_user), Ct);
        _fixture.Gateway.Mailbox.Add(Incoming("a", Pdf()));
        var inbox = await OpenFirstMessageAsync();

        var row = Assert.Single(inbox.AttachmentRows);
        row.SaveCommand.Execute(null);
        await Task.Delay(100, Ct);

        Assert.Equal("a.pdf (4 B)", row.Label);
        Assert.Equal("a.pdf", _fixture.FileSave.SavedName);
        Assert.Equal([1, 2, 3, 4], _fixture.FileSave.SavedContent);
    }

    [Fact]
    public async Task AutoRefreshFetchesQuietlyAndOnlyWhenSwitchedOn()
    {
        await _fixture.Create.HandleAsync(EmailFixture.ValidPop3Account(_user), Ct);
        var inbox = CreateInbox();
        await inbox.LoadCommand.ExecuteAsync(null);

        inbox.AutoRefresh = false;
        _fixture.Gateway.Mailbox.Add(Incoming("a"));
        await inbox.AutoRefreshCommand.ExecuteAsync(null);
        Assert.Empty(inbox.Messages);
        Assert.Empty(_fixture.Gateway.Connections);

        inbox.AutoRefresh = true;
        await inbox.AutoRefreshCommand.ExecuteAsync(null);
        Assert.Single(inbox.Messages);

        var status = inbox.StatusMessage;
        await inbox.AutoRefreshCommand.ExecuteAsync(null);
        Assert.Equal(status, inbox.StatusMessage);
    }

    [Fact]
    public async Task AutoRefreshKeepsTheOpenMessageAndReportsFailures()
    {
        await _fixture.Create.HandleAsync(EmailFixture.ValidPop3Account(_user), Ct);
        _fixture.Gateway.Mailbox.Add(Incoming("a"));
        var inbox = await OpenFirstMessageAsync();
        _fixture.Gateway.Mailbox.Add(Incoming("b"));

        await inbox.AutoRefreshCommand.ExecuteAsync(null);
        await Task.Delay(100, Ct);

        Assert.Equal(2, inbox.Messages.Count);
        Assert.Equal("Hello a", inbox.OpenMessage?.Subject);

        _fixture.Gateway.Failure = new MailGatewayException("email_connection_failed", "down");
        await inbox.AutoRefreshCommand.ExecuteAsync(null);
        Assert.True(inbox.StatusIsError);
    }

    [Fact]
    public async Task ComposeAttachesPickedFilesAndSendsThemAsHtmlWhenAsked()
    {
        await _fixture.Create.HandleAsync(EmailFixture.ValidAccount(_user), Ct);
        var compose = CreateEmbeddedCompose();
        await compose.LoadCommand.ExecuteAsync(null);
        _fixture.FilePick.Next.AddRange([new PickedFile("one.txt", new byte[] { 1 }), new PickedFile("two.txt", new byte[] { 2, 2 })]);

        await compose.AddAttachmentsCommand.ExecuteAsync(null);
        compose.Attachments[0].RemoveCommand.Execute(null);
        compose.To = "bo@example.com";
        compose.IsHtml = true;
        compose.Body = "<p>x</p>";
        await compose.SendCommand.ExecuteAsync(null);

        var sent = Assert.Single(_fixture.Gateway.Sent);
        Assert.True(sent.IsHtml);
        Assert.Equal("two.txt", Assert.Single(sent.Attachments).FileName);
        Assert.Empty(compose.Attachments);
        Assert.False(compose.IsHtml);
    }

    [Fact]
    public async Task SentPageListsReadsAndDeletesSentMessages()
    {
        var account = (await _fixture.Create.HandleAsync(EmailFixture.ValidAccount(_user), Ct)).Value;
        await _fixture.Send.HandleAsync(new SendEmail.Command(_user, account.Id, "bo@example.com", "Report", "Text", false, [new AttachmentInput("r.txt", [1])]), Ct);
        var sent = CreateSent();

        await sent.LoadCommand.ExecuteAsync(null);
        await Task.Delay(100, Ct);
        sent.SelectedMessage = Assert.Single(sent.Messages);
        await Task.Delay(100, Ct);

        Assert.Equal("1 sent message(s)", sent.StatusMessage);
        Assert.Equal("Text", sent.OpenMessage?.Body);
        Assert.Equal("r.txt (1 B)", Assert.Single(sent.AttachmentRows).Label);

        sent.RequestDeleteCommand.Execute(null);
        await sent.ConfirmDeleteCommand.ExecuteAsync(null);
        Assert.Empty(sent.Messages);
        Assert.Null(sent.OpenMessage);
    }
}
