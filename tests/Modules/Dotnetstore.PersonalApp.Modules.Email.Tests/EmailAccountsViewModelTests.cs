using Dotnetstore.PersonalApp.Modules.Email.Domain;
using Dotnetstore.PersonalApp.Modules.Email.Features.EmailAccounts;
using Dotnetstore.PersonalApp.Modules.Email.Resources;
using Dotnetstore.PersonalApp.Shared.Localization;

namespace Dotnetstore.PersonalApp.Modules.Email.Tests;

public sealed class EmailAccountsViewModelTests : IDisposable
{
    private readonly EmailFixture _fixture = new();
    private readonly Guid _user = Guid.NewGuid();
    private readonly Localizer _localization = new();

    public EmailAccountsViewModelTests()
    {
        _localization.AddResources(EmailStrings.ResourceManager);
        _localization.SetCulture("en");
    }

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public void Dispose() => _fixture.Dispose();

    private EmailAccountsViewModel CreateViewModel() =>
        new(_user, _fixture.List, _fixture.Create, _fixture.Update, _fixture.Test, _fixture.Delete, _localization);

    private EmailAccountEditorViewModel CreateEditor() =>
        new(null, _user, _fixture.Create, _fixture.Update, _fixture.Test, _localization);

    [Fact]
    public async Task LoadShowsTheUsersAndTheSystemAccountsWithTranslatedTexts()
    {
        await _fixture.Create.HandleAsync(EmailFixture.ValidAccount(_user, "Mine"), Ct);
        await _fixture.Create.HandleAsync(EmailFixture.ValidAccount(_user, "Shared", isSystem: true), Ct);
        await _fixture.Create.HandleAsync(EmailFixture.ValidAccount(Guid.NewGuid(), "Theirs"), Ct);
        var viewModel = CreateViewModel();

        await viewModel.LoadCommand.ExecuteAsync(null);

        viewModel.Accounts.Select(a => a.Name).ShouldBe(["Mine", "Shared"]);
        viewModel.Accounts[0].ScopeText.ShouldBe("Personal");
        viewModel.Accounts[1].ScopeText.ShouldBe("System");
        viewModel.Accounts[0].ProtocolText.ShouldBe("SMTP (send)");
        viewModel.Accounts[0].Server.ShouldBe("smtp.example.com:587");
        viewModel.StatusMessage.ShouldBe("2 account(s)");
    }

    [Fact]
    public async Task NewThenSaveAddsTheAccountAndClosesTheEditor()
    {
        var viewModel = CreateViewModel();
        viewModel.NewCommand.Execute(null);
        viewModel.Editor.ShouldNotBeNull();

        viewModel.Editor.Name = "Work outgoing";
        viewModel.Editor.Host = "smtp.example.com";
        viewModel.Editor.EmailAddress = "anna@example.com";
        await viewModel.Editor.SaveCommand.ExecuteAsync(null);
        await Task.Delay(100, Ct);

        viewModel.Editor.ShouldBeNull();
        viewModel.Accounts.ShouldHaveSingleItem().Name.ShouldBe("Work outgoing");
    }

    [Fact]
    public async Task SaveWithoutANameShowsTheTranslatedError()
    {
        var editor = CreateEditor();

        await editor.SaveCommand.ExecuteAsync(null);

        editor.ErrorMessage.ShouldBe("Name is required.");
    }

    [Fact]
    public async Task SaveWithAnUnreadablePortShowsThePortError()
    {
        var editor = CreateEditor();
        editor.Name = "Work";
        editor.Host = "smtp.example.com";
        editor.EmailAddress = "anna@example.com";
        editor.PortText = "abc";

        await editor.SaveCommand.ExecuteAsync(null);

        editor.ErrorMessage.ShouldBe("Port must be a number between 1 and 65535.");
    }

    [Fact]
    public void PortFollowsProtocolAndSecurityUntilTheUserTypesAnother()
    {
        var editor = CreateEditor();
        editor.PortText.ShouldBe("587");

        editor.SelectedSecurity = editor.Securities.First(s => s.Value == ConnectionSecurity.SslOnConnect);
        editor.PortText.ShouldBe("465");

        editor.SelectedProtocol = editor.Protocols.First(p => p.Value == MailProtocol.Pop3);
        editor.PortText.ShouldBe("995");

        editor.PortText = "2525";
        editor.SelectedSecurity = editor.Securities.First(s => s.Value == ConnectionSecurity.None);
        editor.PortText.ShouldBe("2525");
    }

    [Fact]
    public async Task EditingAnExistingAccountDoesNotOfferToChangeScopeAndKeepsThePassword()
    {
        var created = (await _fixture.Create.HandleAsync(EmailFixture.ValidAccount(_user), Ct)).Value;
        var editor = new EmailAccountEditorViewModel(created, _user, _fixture.Create, _fixture.Update, _fixture.Test, _localization);

        editor.Name = "Renamed";
        await editor.SaveCommand.ExecuteAsync(null);

        editor.CanChooseScope.ShouldBeFalse();
        editor.PasswordPlaceholder.ShouldBe("Leave empty to keep the current password");
        editor.ErrorMessage.ShouldBeNull();
        (await _fixture.StoredPasswordAsync(created.Id)).ShouldBe("protected:s3cret");
    }

    [Fact]
    public async Task DeleteAsksForConfirmationThenRemovesTheAccount()
    {
        await _fixture.Create.HandleAsync(EmailFixture.ValidAccount(_user), Ct);
        var viewModel = CreateViewModel();
        await viewModel.LoadCommand.ExecuteAsync(null);
        viewModel.SelectedAccount = viewModel.Accounts[0];

        viewModel.RequestDeleteCommand.Execute(null);
        viewModel.IsConfirmingDelete.ShouldBeTrue();
        await viewModel.ConfirmDeleteCommand.ExecuteAsync(null);

        viewModel.Accounts.ShouldBeEmpty();
        viewModel.IsConfirmingDelete.ShouldBeFalse();
    }
}
