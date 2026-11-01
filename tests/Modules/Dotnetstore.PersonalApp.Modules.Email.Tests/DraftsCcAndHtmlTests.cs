using Dotnetstore.PersonalApp.Modules.Email.Features.EmailAccounts;
using Dotnetstore.PersonalApp.Modules.Email.Features.Messages;
using Dotnetstore.PersonalApp.Modules.Email.Mail;
using Dotnetstore.PersonalApp.Modules.Email.Resources;
using Dotnetstore.PersonalApp.Shared.Localization;

namespace Dotnetstore.PersonalApp.Modules.Email.Tests;

public sealed class DraftsCcAndHtmlTests : IDisposable
{
    private readonly EmailFixture _fixture = new();
    private readonly Guid _user = Guid.NewGuid();
    private readonly Localizer _localization = new();

    public DraftsCcAndHtmlTests()
    {
        _localization.AddResources(EmailStrings.ResourceManager);
        _localization.SetCulture("en");
    }

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public void Dispose() => _fixture.Dispose();

    private static EmailMessageDto Message(string from, string to, string cc) =>
        new(Guid.NewGuid(), Guid.NewGuid(), from, to, cc, "Hello", DateTimeOffset.UnixEpoch, "Body", null, []);

    private SaveDraft.Command Draft(Guid? draftId = null, string to = "bo@example.com", string body = "Hi", IReadOnlyList<AttachmentInput>? attachments = null, Guid? user = null) =>
        new(user ?? _user, draftId, null, to, string.Empty, "Subject", body, false, attachments);

    [Fact]
    public void ReplyAllKeepsSenderOtherRecipientsAndCcButNotUsOrRepeats()
    {
        var (to, cc) = InboxViewModel.ReplyAllRecipients(
            Message("Bo <bo@example.com>", "anna@example.com, cy@example.com, BO@example.com", "di@example.com; Cy@example.com, anna@example.com"),
            "Anna@Example.com");

        Assert.Equal("bo@example.com, cy@example.com", to);
        Assert.Equal("di@example.com", cc);
    }

    [Fact]
    public void ReplyAllOnOldMailWithoutRecipientsIsAPlainReply()
    {
        var (to, cc) = InboxViewModel.ReplyAllRecipients(Message("Bo <bo@example.com>", string.Empty, string.Empty), "anna@example.com");

        Assert.Equal("bo@example.com", to);
        Assert.Equal(string.Empty, cc);
    }

    [Fact]
    public async Task SendPassesCcOnAndLeavesOutCcAlreadyInTo()
    {
        var account = (await _fixture.Create.HandleAsync(EmailFixture.ValidAccount(_user), Ct)).Value;

        var result = await _fixture.Send.HandleAsync(
            new SendEmail.Command(_user, account.Id, "bo@example.com", "Hi", "Text", false, null, "cy@example.com, bo@example.com"),
            Ct);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value);
        var sent = Assert.Single(_fixture.Gateway.Sent);
        Assert.Equal(["cy@example.com"], sent.Cc);

        var stored = Assert.Single((await _fixture.ListSent.HandleAsync(new ListSentMessages.Query(_user, account.Id), Ct)).Value.Items);
        Assert.Equal("cy@example.com", stored.Cc);
    }

    [Fact]
    public async Task SendRefusesAnInvalidCcAddress()
    {
        var account = (await _fixture.Create.HandleAsync(EmailFixture.ValidAccount(_user), Ct)).Value;

        var result = await _fixture.Send.HandleAsync(
            new SendEmail.Command(_user, account.Id, "bo@example.com", "Hi", "Text", false, null, "not an address"),
            Ct);

        Assert.False(result.IsSuccess);
        Assert.Empty(_fixture.Gateway.Sent);
    }

    [Fact]
    public async Task FetchStoresRecipientsAndHtml()
    {
        var account = (await _fixture.Create.HandleAsync(EmailFixture.ValidPop3Account(_user), Ct)).Value;
        _fixture.Gateway.Mailbox.Add(new FetchedMessage(
            "u1", "Bo <bo@example.com>", "Hello", DateTimeOffset.UnixEpoch, "Plain", [],
            ["anna@example.com", "cy@example.com"], ["di@example.com"], "<p>Rich</p>"));

        await _fixture.Fetch.HandleAsync(new FetchEmail.Command(_user, account.Id), Ct);

        var summary = Assert.Single((await _fixture.ListMessages.HandleAsync(new ListEmailMessages.Query(_user, account.Id), Ct)).Value.Items);
        var message = (await _fixture.GetMessage.HandleAsync(new GetEmailMessage.Query(summary.Id, _user), Ct)).Value;
        Assert.Equal("anna@example.com, cy@example.com", message.To);
        Assert.Equal("di@example.com", message.Cc);
        Assert.Equal("<p>Rich</p>", message.HtmlBody);
    }

    [Fact]
    public async Task SavingADraftThenSavingAgainReplacesItAndItsAttachments()
    {
        var first = (await _fixture.SaveDraft.HandleAsync(Draft(attachments: [new AttachmentInput("a.txt", [1])]), Ct)).Value;

        var second = await _fixture.SaveDraft.HandleAsync(Draft(first.Id, body: "Changed", attachments: [new AttachmentInput("b.txt", [2, 2])]), Ct);

        Assert.True(second.IsSuccess);
        Assert.Equal(first.Id, second.Value.Id);
        Assert.Single((await _fixture.ListDrafts.HandleAsync(new ListDrafts.Query(_user), Ct)).Items);
        var full = (await _fixture.GetDraft.HandleAsync(new GetDraft.Query(first.Id, _user), Ct)).Value;
        Assert.Equal("Changed", full.Body);
        Assert.Equal("b.txt", Assert.Single(full.Attachments).FileName);
    }

    [Fact]
    public async Task AnEmptyDraftIsRefused()
    {
        var result = await _fixture.SaveDraft.HandleAsync(new SaveDraft.Command(_user, null, null, string.Empty, string.Empty, string.Empty, string.Empty, false, null), Ct);

        Assert.Equal("email_draft_empty", result.Error.Code);
    }

    [Fact]
    public async Task DraftsAreInvisibleAndUntouchableForOtherUsers()
    {
        var other = Guid.NewGuid();
        var draft = (await _fixture.SaveDraft.HandleAsync(Draft(attachments: [new AttachmentInput("a.txt", [1])]), Ct)).Value;
        var attachment = (await _fixture.GetDraft.HandleAsync(new GetDraft.Query(draft.Id, _user), Ct)).Value.Attachments[0];

        Assert.Empty((await _fixture.ListDrafts.HandleAsync(new ListDrafts.Query(other), Ct)).Items);
        Assert.Equal("email_draft_not_found", (await _fixture.GetDraft.HandleAsync(new GetDraft.Query(draft.Id, other), Ct)).Error.Code);
        Assert.Equal("email_draft_not_found", (await _fixture.SaveDraft.HandleAsync(Draft(draft.Id, user: other), Ct)).Error.Code);
        Assert.Equal("email_draft_not_found", (await _fixture.DeleteDraft.HandleAsync(new DeleteDraft.Command(draft.Id, other), Ct)).Error.Code);
        Assert.False((await _fixture.GetAttachment.HandleAsync(new GetEmailAttachment.Query(attachment.Id, other), Ct)).IsSuccess);
        Assert.True((await _fixture.GetAttachment.HandleAsync(new GetEmailAttachment.Query(attachment.Id, _user), Ct)).IsSuccess);
    }

    [Fact]
    public async Task DeletingADraftRemovesIt()
    {
        var draft = (await _fixture.SaveDraft.HandleAsync(Draft(), Ct)).Value;

        Assert.True((await _fixture.DeleteDraft.HandleAsync(new DeleteDraft.Command(draft.Id, _user), Ct)).IsSuccess);

        Assert.Empty((await _fixture.ListDrafts.HandleAsync(new ListDrafts.Query(_user), Ct)).Items);
    }

    [Fact]
    public async Task DraftsGoWhenTheUserIsDeleted()
    {
        await _fixture.SaveDraft.HandleAsync(Draft(), Ct);

        await _fixture.DeleteForUser.HandleAsync(new DeleteEmailAccountsForUser.Command(_user), Ct);

        Assert.Empty((await _fixture.ListDrafts.HandleAsync(new ListDrafts.Query(_user), Ct)).Items);
    }

    private ComposeViewModel CreateCompose() =>
        new(_user, true, _fixture.List, _fixture.Send, _fixture.SaveDraft, _fixture.DeleteDraft, _fixture.FilePick, _localization);

    [Fact]
    public async Task ComposerSavesADraftAndSendingItDeletesTheDraft()
    {
        await _fixture.Create.HandleAsync(EmailFixture.ValidAccount(_user), Ct);
        var composer = CreateCompose();
        await composer.LoadCommand.ExecuteAsync(null);
        composer.To = "bo@example.com";
        composer.Subject = "Plan";
        composer.Body = "Text";

        await composer.SaveDraftCommand.ExecuteAsync(null);

        Assert.NotNull(composer.DraftId);
        Assert.Equal("Draft saved.", composer.StatusMessage);
        Assert.Single((await _fixture.ListDrafts.HandleAsync(new ListDrafts.Query(_user), Ct)).Items);

        await composer.SendCommand.ExecuteAsync(null);

        Assert.Null(composer.DraftId);
        Assert.Empty((await _fixture.ListDrafts.HandleAsync(new ListDrafts.Query(_user), Ct)).Items);
        Assert.Single(_fixture.Gateway.Sent);
    }

    [Fact]
    public async Task DraftsViewModelOpensADraftInTheComposerAndDeletesIt()
    {
        await _fixture.Create.HandleAsync(EmailFixture.ValidAccount(_user), Ct);
        await _fixture.SaveDraft.HandleAsync(Draft(attachments: [new AttachmentInput("a.txt", [1, 2])]), Ct);
        var drafts = new DraftsViewModel(_user, _fixture.ListDrafts, _fixture.GetDraft, _fixture.DeleteDraft, _fixture.GetAttachment, CreateCompose, _localization);

        await drafts.LoadCommand.ExecuteAsync(null);
        drafts.SelectedDraft = drafts.Drafts[0];
        await Task.Delay(200, Ct);

        Assert.NotNull(drafts.Composer);
        Assert.Equal("bo@example.com", drafts.Composer.To);
        Assert.Equal("a.txt", Assert.Single(drafts.Composer.Attachments).FileName);

        await drafts.ConfirmDeleteCommand.ExecuteAsync(null);

        Assert.Empty(drafts.Drafts);
        Assert.Null(drafts.Composer);
    }

    [Fact]
    public void FormatterKeepsStructureAndStyle()
    {
        var blocks = HtmlFormatter.Format("<h1>Title</h1><p>Hello <b>bold</b> and <i>italic</i> <a href=\"https://example.com\">link</a></p><ul><li>One</li><li>Two</li></ul>");

        Assert.Equal(4, blocks.Count);
        Assert.Equal(1, blocks[0].HeadingLevel);
        Assert.Contains(blocks[1].Runs, r => r.Bold && r.Text == "bold");
        Assert.Contains(blocks[1].Runs, r => r.Italic && r.Text == "italic");
        Assert.Contains(blocks[1].Runs, r => r.Href == "https://example.com" && r.Underline);
        Assert.StartsWith("• One", blocks[2].Runs[0].Text, StringComparison.Ordinal);
    }

    [Fact]
    public void FormatterDropsScriptsAndStylesAndDoesNotLoadImages()
    {
        var blocks = HtmlFormatter.Format(
            "<style>p { color: red }</style><script>alert(1)</script><p>Safe</p><img src=\"https://tracker.example/p.gif\" alt=\"Logo\"><a href=\"javascript:alert(1)\">bad</a>");

        var text = string.Concat(blocks.SelectMany(b => b.Runs).Select(r => r.Text));
        Assert.Contains("Safe", text, StringComparison.Ordinal);
        Assert.Contains("[Logo]", text, StringComparison.Ordinal);
        Assert.DoesNotContain("alert", text, StringComparison.Ordinal);
        Assert.DoesNotContain("color", text, StringComparison.Ordinal);
        Assert.DoesNotContain("tracker.example", text, StringComparison.Ordinal);
        Assert.All(blocks.SelectMany(b => b.Runs), r => Assert.Null(r.Href));
    }

    [Fact]
    public void FormatterOfNothingIsEmpty()
    {
        Assert.Empty(HtmlFormatter.Format(null));
        Assert.Empty(HtmlFormatter.Format("   "));
    }
}
