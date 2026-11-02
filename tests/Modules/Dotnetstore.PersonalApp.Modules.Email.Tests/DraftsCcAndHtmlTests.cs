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

        to.ShouldBe("bo@example.com, cy@example.com");
        cc.ShouldBe("di@example.com");
    }

    [Fact]
    public void ReplyAllOnOldMailWithoutRecipientsIsAPlainReply()
    {
        var (to, cc) = InboxViewModel.ReplyAllRecipients(Message("Bo <bo@example.com>", string.Empty, string.Empty), "anna@example.com");

        to.ShouldBe("bo@example.com");
        cc.ShouldBe(string.Empty);
    }

    [Fact]
    public async Task SendPassesCcOnAndLeavesOutCcAlreadyInTo()
    {
        var account = (await _fixture.Create.HandleAsync(EmailFixture.ValidAccount(_user), Ct)).Value;

        var result = await _fixture.Send.HandleAsync(
            new SendEmail.Command(_user, account.Id, "bo@example.com", "Hi", "Text", false, null, "cy@example.com, bo@example.com"),
            Ct);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(2);
        var sent = _fixture.Gateway.Sent.ShouldHaveSingleItem();
        sent.Cc.ShouldBe(["cy@example.com"]);

        var stored = (await _fixture.ListSent.HandleAsync(new ListSentMessages.Query(_user, account.Id), Ct)).Value.Items.ShouldHaveSingleItem();
        stored.Cc.ShouldBe("cy@example.com");
    }

    [Fact]
    public async Task SendRefusesAnInvalidCcAddress()
    {
        var account = (await _fixture.Create.HandleAsync(EmailFixture.ValidAccount(_user), Ct)).Value;

        var result = await _fixture.Send.HandleAsync(
            new SendEmail.Command(_user, account.Id, "bo@example.com", "Hi", "Text", false, null, "not an address"),
            Ct);

        result.IsSuccess.ShouldBeFalse();
        _fixture.Gateway.Sent.ShouldBeEmpty();
    }

    [Fact]
    public async Task FetchStoresRecipientsAndHtml()
    {
        var account = (await _fixture.Create.HandleAsync(EmailFixture.ValidPop3Account(_user), Ct)).Value;
        _fixture.Gateway.Mailbox.Add(new FetchedMessage(
            "u1", "Bo <bo@example.com>", "Hello", DateTimeOffset.UnixEpoch, "Plain", [],
            ["anna@example.com", "cy@example.com"], ["di@example.com"], "<p>Rich</p>"));

        await _fixture.Fetch.HandleAsync(new FetchEmail.Command(_user, account.Id), Ct);

        var summary = (await _fixture.ListMessages.HandleAsync(new ListEmailMessages.Query(_user, account.Id), Ct)).Value.Items.ShouldHaveSingleItem();
        var message = (await _fixture.GetMessage.HandleAsync(new GetEmailMessage.Query(summary.Id, _user), Ct)).Value;
        message.To.ShouldBe("anna@example.com, cy@example.com");
        message.Cc.ShouldBe("di@example.com");
        message.HtmlBody.ShouldBe("<p>Rich</p>");
    }

    [Fact]
    public async Task SavingADraftThenSavingAgainReplacesItAndItsAttachments()
    {
        var first = (await _fixture.SaveDraft.HandleAsync(Draft(attachments: [new AttachmentInput("a.txt", [1])]), Ct)).Value;

        var second = await _fixture.SaveDraft.HandleAsync(Draft(first.Id, body: "Changed", attachments: [new AttachmentInput("b.txt", [2, 2])]), Ct);

        second.IsSuccess.ShouldBeTrue();
        second.Value.Id.ShouldBe(first.Id);
        (await _fixture.ListDrafts.HandleAsync(new ListDrafts.Query(_user), Ct)).Items.ShouldHaveSingleItem();
        var full = (await _fixture.GetDraft.HandleAsync(new GetDraft.Query(first.Id, _user), Ct)).Value;
        full.Body.ShouldBe("Changed");
        full.Attachments.ShouldHaveSingleItem().FileName.ShouldBe("b.txt");
    }

    [Fact]
    public async Task AnEmptyDraftIsRefused()
    {
        var result = await _fixture.SaveDraft.HandleAsync(new SaveDraft.Command(_user, null, null, string.Empty, string.Empty, string.Empty, string.Empty, false, null), Ct);

        result.Error.Code.ShouldBe("email_draft_empty");
    }

    [Fact]
    public async Task DraftsAreInvisibleAndUntouchableForOtherUsers()
    {
        var other = Guid.NewGuid();
        var draft = (await _fixture.SaveDraft.HandleAsync(Draft(attachments: [new AttachmentInput("a.txt", [1])]), Ct)).Value;
        var attachment = (await _fixture.GetDraft.HandleAsync(new GetDraft.Query(draft.Id, _user), Ct)).Value.Attachments[0];

        (await _fixture.ListDrafts.HandleAsync(new ListDrafts.Query(other), Ct)).Items.ShouldBeEmpty();
        (await _fixture.GetDraft.HandleAsync(new GetDraft.Query(draft.Id, other), Ct)).Error.Code.ShouldBe("email_draft_not_found");
        (await _fixture.SaveDraft.HandleAsync(Draft(draft.Id, user: other), Ct)).Error.Code.ShouldBe("email_draft_not_found");
        (await _fixture.DeleteDraft.HandleAsync(new DeleteDraft.Command(draft.Id, other), Ct)).Error.Code.ShouldBe("email_draft_not_found");
        (await _fixture.GetAttachment.HandleAsync(new GetEmailAttachment.Query(attachment.Id, other), Ct)).IsSuccess.ShouldBeFalse();
        (await _fixture.GetAttachment.HandleAsync(new GetEmailAttachment.Query(attachment.Id, _user), Ct)).IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public async Task DeletingADraftRemovesIt()
    {
        var draft = (await _fixture.SaveDraft.HandleAsync(Draft(), Ct)).Value;

        (await _fixture.DeleteDraft.HandleAsync(new DeleteDraft.Command(draft.Id, _user), Ct)).IsSuccess.ShouldBeTrue();

        (await _fixture.ListDrafts.HandleAsync(new ListDrafts.Query(_user), Ct)).Items.ShouldBeEmpty();
    }

    [Fact]
    public async Task DraftsGoWhenTheUserIsDeleted()
    {
        await _fixture.SaveDraft.HandleAsync(Draft(), Ct);

        await _fixture.DeleteForUser.HandleAsync(new DeleteEmailAccountsForUser.Command(_user), Ct);

        (await _fixture.ListDrafts.HandleAsync(new ListDrafts.Query(_user), Ct)).Items.ShouldBeEmpty();
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

        composer.DraftId.ShouldNotBeNull();
        composer.StatusMessage.ShouldBe("Draft saved.");
        (await _fixture.ListDrafts.HandleAsync(new ListDrafts.Query(_user), Ct)).Items.ShouldHaveSingleItem();

        await composer.SendCommand.ExecuteAsync(null);

        composer.DraftId.ShouldBeNull();
        (await _fixture.ListDrafts.HandleAsync(new ListDrafts.Query(_user), Ct)).Items.ShouldBeEmpty();
        _fixture.Gateway.Sent.ShouldHaveSingleItem();
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

        drafts.Composer.ShouldNotBeNull();
        drafts.Composer.To.ShouldBe("bo@example.com");
        drafts.Composer.Attachments.ShouldHaveSingleItem().FileName.ShouldBe("a.txt");

        await drafts.ConfirmDeleteCommand.ExecuteAsync(null);

        drafts.Drafts.ShouldBeEmpty();
        drafts.Composer.ShouldBeNull();
    }

    [Fact]
    public void FormatterKeepsStructureAndStyle()
    {
        var blocks = HtmlFormatter.Format("<h1>Title</h1><p>Hello <b>bold</b> and <i>italic</i> <a href=\"https://example.com\">link</a></p><ul><li>One</li><li>Two</li></ul>");

        blocks.Count.ShouldBe(4);
        blocks[0].HeadingLevel.ShouldBe(1);
        blocks[1].Runs.ShouldContain(r => r.Bold && r.Text == "bold");
        blocks[1].Runs.ShouldContain(r => r.Italic && r.Text == "italic");
        blocks[1].Runs.ShouldContain(r => r.Href == "https://example.com" && r.Underline);
        (blocks[2].Runs[0].Text).ShouldStartWith("• One", Case.Sensitive);
    }

    [Fact]
    public void FormatterDropsScriptsAndStylesAndDoesNotLoadImages()
    {
        var blocks = HtmlFormatter.Format(
            "<style>p { color: red }</style><script>alert(1)</script><p>Safe</p><img src=\"https://tracker.example/p.gif\" alt=\"Logo\"><a href=\"javascript:alert(1)\">bad</a>");

        var text = string.Concat(blocks.SelectMany(b => b.Runs).Select(r => r.Text));
        text.ShouldContain("Safe", Case.Sensitive);
        text.ShouldContain("[Logo]", Case.Sensitive);
        text.ShouldNotContain("alert", Case.Sensitive);
        text.ShouldNotContain("color", Case.Sensitive);
        text.ShouldNotContain("tracker.example", Case.Sensitive);
        blocks.SelectMany(b => b.Runs).ShouldAllBe(r => r.Href == null);
    }

    [Fact]
    public void FormatterOfNothingIsEmpty()
    {
        HtmlFormatter.Format(null).ShouldBeEmpty();
        HtmlFormatter.Format("   ").ShouldBeEmpty();
    }
}
