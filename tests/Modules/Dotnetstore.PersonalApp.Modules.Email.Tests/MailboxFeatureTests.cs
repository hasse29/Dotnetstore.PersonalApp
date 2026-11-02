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

        result.IsSuccess.ShouldBeTrue();
        var sent = _fixture.Gateway.Sent.ShouldHaveSingleItem();
        sent.IsHtml.ShouldBeTrue();
        var attachment = sent.Attachments.ShouldHaveSingleItem();
        attachment.ContentType.ShouldBe("application/pdf");

        var list = (await _fixture.ListSent.HandleAsync(new ListSentMessages.Query(_user, account.Id), Ct)).Value;
        var full = (await _fixture.GetSent.HandleAsync(new GetSentMessage.Query(list.Items[0].Id, _user), Ct)).Value;
        list.Items[0].Body.ShouldBe(string.Empty);
        full.Body.ShouldBe("<b>Hi</b>");
        full.IsHtml.ShouldBeTrue();
        full.To.ShouldBe("bo@example.com");
        (full.Attachments.ShouldHaveSingleItem().FileName, full.Attachments.ShouldHaveSingleItem().Size).ShouldBe(("report.pdf", 2));
    }

    [Fact]
    public async Task AFailedSendLeavesNoCopy()
    {
        var account = (await _fixture.Create.HandleAsync(EmailFixture.ValidAccount(_user), Ct)).Value;
        _fixture.Gateway.Failure = new MailGatewayException("email_connection_failed", "down");

        await _fixture.Send.HandleAsync(new SendEmail.Command(_user, account.Id, "bo@example.com", "Hi", "x"), Ct);

        var list = (await _fixture.ListSent.HandleAsync(new ListSentMessages.Query(_user, account.Id), Ct)).Value;
        list.TotalCount.ShouldBe(0);
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

        tooMany.Error.Code.ShouldBe("email_too_many_attachments");
        tooBig.Error.Code.ShouldBe("email_attachment_too_large");
        tooBigTogether.Error.Code.ShouldBe("email_attachments_too_large");
        _fixture.Gateway.Sent.ShouldBeEmpty();
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

        listed.Error.Code.ShouldBe("email_account_not_found");
        got.Error.Code.ShouldBe("email_message_not_found");
        deletedByStranger.Error.Code.ShouldBe("email_message_not_found");
        deleted.IsSuccess.ShouldBeTrue();
        (await _fixture.ListSent.HandleAsync(new ListSentMessages.Query(_user, mine.Id), Ct)).Value.TotalCount.ShouldBe(0);
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

        message.Attachments.Select(a => a.FileName).ShouldBe(["a.pdf", "b.pdf"]);
        message.Attachments[0].Size.ShouldBe(4);
        content.Value.Content.ShouldBe([1, 2, 3, 4]);
        content.Value.ContentType.ShouldBe("application/pdf");
        stranger.Error.Code.ShouldBe("email_attachment_not_found");
        missing.Error.Code.ShouldBe("email_attachment_not_found");
    }

    [Fact]
    public async Task FetchSkipsOversizedAttachments()
    {
        var account = (await _fixture.Create.HandleAsync(EmailFixture.ValidPop3Account(_user), Ct)).Value;
        _fixture.Gateway.Mailbox.Add(Incoming("a", Pdf(), new MailAttachmentData("big.bin", "application/octet-stream", new byte[MailLimits.MaxAttachmentBytes + 1])));
        await _fixture.Fetch.HandleAsync(new FetchEmail.Command(_user, account.Id), Ct);
        var summary = (await _fixture.ListMessages.HandleAsync(new ListEmailMessages.Query(_user, account.Id), Ct)).Value.Items[0];

        var message = (await _fixture.GetMessage.HandleAsync(new GetEmailMessage.Query(summary.Id, _user), Ct)).Value;

        message.Attachments.ShouldHaveSingleItem().FileName.ShouldBe("a.pdf");
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

        deleted.IsSuccess.ShouldBeTrue();
        refetched.Value.ShouldBe(0);
        again.Error.Code.ShouldBe("email_message_not_found");
        (await _fixture.MessageCountAsync()).ShouldBe(1);
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

        stranger.Error.Code.ShouldBe("email_message_not_found");
        attachment.Error.Code.ShouldBe("email_attachment_not_found");
    }

    [Fact]
    public void MessageRulesPickTheAddressAndAvoidDoublePrefixes()
    {
        MessageRules.ExtractAddress("Bo <bo@example.com>").ShouldBe("bo@example.com");
        MessageRules.ExtractAddress("bo@example.com").ShouldBe("bo@example.com");
        MessageRules.WithPrefix("Re: ", "Hi").ShouldBe("Re: Hi");
        MessageRules.WithPrefix("Re: ", "RE: Hi").ShouldBe("RE: Hi");
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

        var composer = inbox.Composer.ShouldBeOfType<ComposeViewModel>();
        composer.To.ShouldBe("bo@example.com");
        composer.Subject.ShouldBe("Re: Hello a");
        composer.Body.ShouldContain("> Line one\n> Line two", Case.Sensitive);
        composer.Body.ShouldContain("Bo <bo@example.com> wrote:", Case.Sensitive);
        (composer.SelectedAccount?.EmailAddress).ShouldBe("anna@example.com");
        composer.IsEmbedded.ShouldBeTrue();
    }

    [Fact]
    public async Task ForwardKeepsTheAttachmentsAndSendingClosesTheComposer()
    {
        await _fixture.Create.HandleAsync(EmailFixture.ValidAccount(_user), Ct);
        await _fixture.Create.HandleAsync(EmailFixture.ValidPop3Account(_user), Ct);
        _fixture.Gateway.Mailbox.Add(Incoming("a", Pdf()));
        var inbox = await OpenFirstMessageAsync();

        await inbox.ForwardCommand.ExecuteAsync(null);
        var composer = inbox.Composer.ShouldBeOfType<ComposeViewModel>();
        composer.To = "cy@example.com";
        await composer.SendCommand.ExecuteAsync(null);

        inbox.Composer.ShouldBeNull();
        var sent = _fixture.Gateway.Sent.ShouldHaveSingleItem();
        sent.Subject.ShouldBe("Fwd: Hello a");
        sent.Body.ShouldContain("Forwarded message", Case.Sensitive);
        sent.Attachments.ShouldHaveSingleItem().FileName.ShouldBe("a.pdf");
        inbox.StatusMessage.ShouldBe("Message sent.");
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

        inbox.Composer.ShouldBeNull();
        inbox.OpenMessage.ShouldNotBeNull();
    }

    [Fact]
    public async Task DeleteInTheInboxAsksThenRemovesTheMessageLocally()
    {
        await _fixture.Create.HandleAsync(EmailFixture.ValidPop3Account(_user), Ct);
        _fixture.Gateway.Mailbox.AddRange([Incoming("a"), Incoming("b")]);
        var inbox = await OpenFirstMessageAsync();

        inbox.RequestDeleteCommand.Execute(null);
        inbox.IsConfirmingDelete.ShouldBeTrue();
        await inbox.ConfirmDeleteCommand.ExecuteAsync(null);

        inbox.Messages.ShouldHaveSingleItem();
        inbox.OpenMessage.ShouldBeNull();
        inbox.IsConfirmingDelete.ShouldBeFalse();
        (await _fixture.MessageCountAsync()).ShouldBe(1);
    }

    [Fact]
    public async Task SavingAnAttachmentHandsItToTheSaveDialog()
    {
        await _fixture.Create.HandleAsync(EmailFixture.ValidPop3Account(_user), Ct);
        _fixture.Gateway.Mailbox.Add(Incoming("a", Pdf()));
        var inbox = await OpenFirstMessageAsync();

        var row = inbox.AttachmentRows.ShouldHaveSingleItem();
        row.SaveCommand.Execute(null);
        await Task.Delay(100, Ct);

        row.Label.ShouldBe("a.pdf (4 B)");
        _fixture.FileSave.SavedName.ShouldBe("a.pdf");
        _fixture.FileSave.SavedContent.ShouldBe([1, 2, 3, 4]);
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
        inbox.Messages.ShouldBeEmpty();
        _fixture.Gateway.Connections.ShouldBeEmpty();

        inbox.AutoRefresh = true;
        await inbox.AutoRefreshCommand.ExecuteAsync(null);
        inbox.Messages.ShouldHaveSingleItem();

        var status = inbox.StatusMessage;
        await inbox.AutoRefreshCommand.ExecuteAsync(null);
        inbox.StatusMessage.ShouldBe(status);
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

        inbox.Messages.Count.ShouldBe(2);
        (inbox.OpenMessage?.Subject).ShouldBe("Hello a");

        _fixture.Gateway.Failure = new MailGatewayException("email_connection_failed", "down");
        await inbox.AutoRefreshCommand.ExecuteAsync(null);
        inbox.StatusIsError.ShouldBeTrue();
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

        var sent = _fixture.Gateway.Sent.ShouldHaveSingleItem();
        sent.IsHtml.ShouldBeTrue();
        sent.Attachments.ShouldHaveSingleItem().FileName.ShouldBe("two.txt");
        compose.Attachments.ShouldBeEmpty();
        compose.IsHtml.ShouldBeFalse();
    }

    [Fact]
    public async Task SentPageListsReadsAndDeletesSentMessages()
    {
        var account = (await _fixture.Create.HandleAsync(EmailFixture.ValidAccount(_user), Ct)).Value;
        await _fixture.Send.HandleAsync(new SendEmail.Command(_user, account.Id, "bo@example.com", "Report", "Text", false, [new AttachmentInput("r.txt", [1])]), Ct);
        var sent = CreateSent();

        await sent.LoadCommand.ExecuteAsync(null);
        await Task.Delay(100, Ct);
        sent.SelectedMessage = sent.Messages.ShouldHaveSingleItem();
        await Task.Delay(100, Ct);

        sent.StatusMessage.ShouldBe("1 sent message(s)");
        (sent.OpenMessage?.Body).ShouldBe("Text");
        sent.AttachmentRows.ShouldHaveSingleItem().Label.ShouldBe("r.txt (1 B)");

        sent.RequestDeleteCommand.Execute(null);
        await sent.ConfirmDeleteCommand.ExecuteAsync(null);
        sent.Messages.ShouldBeEmpty();
        sent.OpenMessage.ShouldBeNull();
    }
}
