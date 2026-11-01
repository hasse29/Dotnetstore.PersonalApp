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

        Assert.Equal(["Mine", "Shared"], viewModel.Accounts.Select(a => a.Name));
        Assert.Equal("Personal", viewModel.Accounts[0].ScopeText);
        Assert.Equal("System", viewModel.Accounts[1].ScopeText);
        Assert.Equal("SMTP (send)", viewModel.Accounts[0].ProtocolText);
        Assert.Equal("smtp.example.com:587", viewModel.Accounts[0].Server);
        Assert.Equal("2 account(s)", viewModel.StatusMessage);
    }

    [Fact]
    public async Task NewThenSaveAddsTheAccountAndClosesTheEditor()
    {
        var viewModel = CreateViewModel();
        viewModel.NewCommand.Execute(null);
        Assert.NotNull(viewModel.Editor);

        viewModel.Editor.Name = "Work outgoing";
        viewModel.Editor.Host = "smtp.example.com";
        viewModel.Editor.EmailAddress = "anna@example.com";
        await viewModel.Editor.SaveCommand.ExecuteAsync(null);
        await Task.Delay(100, Ct);

        Assert.Null(viewModel.Editor);
        Assert.Equal("Work outgoing", Assert.Single(viewModel.Accounts).Name);
    }

    [Fact]
    public async Task SaveWithoutANameShowsTheTranslatedError()
    {
        var editor = CreateEditor();

        await editor.SaveCommand.ExecuteAsync(null);

        Assert.Equal("Name is required.", editor.ErrorMessage);
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

        Assert.Equal("Port must be a number between 1 and 65535.", editor.ErrorMessage);
    }

    [Fact]
    public void PortFollowsProtocolAndSecurityUntilTheUserTypesAnother()
    {
        var editor = CreateEditor();
        Assert.Equal("587", editor.PortText);

        editor.SelectedSecurity = editor.Securities.First(s => s.Value == ConnectionSecurity.SslOnConnect);
        Assert.Equal("465", editor.PortText);

        editor.SelectedProtocol = editor.Protocols.First(p => p.Value == MailProtocol.Pop3);
        Assert.Equal("995", editor.PortText);

        editor.PortText = "2525";
        editor.SelectedSecurity = editor.Securities.First(s => s.Value == ConnectionSecurity.None);
        Assert.Equal("2525", editor.PortText);
    }

    [Fact]
    public async Task EditingAnExistingAccountDoesNotOfferToChangeScopeAndKeepsThePassword()
    {
        var created = (await _fixture.Create.HandleAsync(EmailFixture.ValidAccount(_user), Ct)).Value;
        var editor = new EmailAccountEditorViewModel(created, _user, _fixture.Create, _fixture.Update, _fixture.Test, _localization);

        editor.Name = "Renamed";
        await editor.SaveCommand.ExecuteAsync(null);

        Assert.False(editor.CanChooseScope);
        Assert.Equal("Leave empty to keep the current password", editor.PasswordPlaceholder);
        Assert.Null(editor.ErrorMessage);
        Assert.Equal("protected:s3cret", await _fixture.StoredPasswordAsync(created.Id));
    }

    [Fact]
    public async Task DeleteAsksForConfirmationThenRemovesTheAccount()
    {
        await _fixture.Create.HandleAsync(EmailFixture.ValidAccount(_user), Ct);
        var viewModel = CreateViewModel();
        await viewModel.LoadCommand.ExecuteAsync(null);
        viewModel.SelectedAccount = viewModel.Accounts[0];

        viewModel.RequestDeleteCommand.Execute(null);
        Assert.True(viewModel.IsConfirmingDelete);
        await viewModel.ConfirmDeleteCommand.ExecuteAsync(null);

        Assert.Empty(viewModel.Accounts);
        Assert.False(viewModel.IsConfirmingDelete);
    }
}
