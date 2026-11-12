using Dotnetstore.PersonalApp.Modules.Users.Domain;
using Dotnetstore.PersonalApp.Modules.Users.Features.Authentication;
using Dotnetstore.PersonalApp.Modules.Users.Resources;
using Dotnetstore.PersonalApp.Shared.Auth;
using Dotnetstore.PersonalApp.Shared.Localization;
using Microsoft.AspNetCore.Identity;

namespace Dotnetstore.PersonalApp.Modules.Users.Tests;

public sealed class AuthenticationViewModelTests : IDisposable
{
    private readonly UsersFixture _fixture = new();
    private readonly Localizer _localizer = new();

    public AuthenticationViewModelTests()
    {
        _localizer.AddResources(UsersStrings.ResourceManager);
        _localizer.SetCulture("en");
    }

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public void Dispose() => _fixture.Dispose();

    private LoginViewModel NewLogin(Action<AuthenticatedUser> onLoggedIn) =>
        new(new Login.Handler(_fixture.DbFactory, new PasswordHasher<User>()), _localizer, onLoggedIn);

    private FirstUserViewModel NewFirstUser(Action onCreated) => new(_fixture.Create, _localizer, onCreated);

    [Fact]
    public async Task ALoginWithTheRightPasswordHandsOverTheUserAndClearsThePassword()
    {
        await _fixture.Create.HandleAsync(UsersFixture.ValidCommand(), Ct);
        AuthenticatedUser? signedIn = null;
        var viewModel = NewLogin(user => signedIn = user);
        viewModel.Username = "ada@example.com";
        viewModel.Password = "correct horse battery";

        await viewModel.LoginCommand.ExecuteAsync(null);

        signedIn.ShouldNotBeNull();
        signedIn.Username.ShouldBe("ada@example.com");
        viewModel.Password.ShouldBeEmpty();
        viewModel.ErrorMessage.ShouldBeNull();
    }

    [Fact]
    public async Task AWrongPasswordShowsTheTranslatedErrorAndClearsThePassword()
    {
        await _fixture.Create.HandleAsync(UsersFixture.ValidCommand(), Ct);
        var signedIn = false;
        var viewModel = NewLogin(_ => signedIn = true);
        viewModel.Username = "ada@example.com";
        viewModel.Password = "wrong password";

        await viewModel.LoginCommand.ExecuteAsync(null);

        signedIn.ShouldBeFalse();
        viewModel.ErrorMessage.ShouldBe("Invalid username or password.");
        viewModel.Password.ShouldBeEmpty();
    }

    [Fact]
    public async Task ATryClearsTheErrorOfThePreviousOne()
    {
        await _fixture.Create.HandleAsync(UsersFixture.ValidCommand(), Ct);
        var viewModel = NewLogin(_ => { });
        viewModel.Username = "ada@example.com";
        viewModel.Password = "wrong password";
        await viewModel.LoginCommand.ExecuteAsync(null);

        viewModel.Password = "correct horse battery";
        await viewModel.LoginCommand.ExecuteAsync(null);

        viewModel.ErrorMessage.ShouldBeNull();
    }

    [Fact]
    public async Task TheFirstUserNeedsADateOfBirth()
    {
        var created = false;
        var viewModel = NewFirstUser(() => created = true);

        await viewModel.CreateCommand.ExecuteAsync(null);

        created.ShouldBeFalse();
        viewModel.ErrorMessage.ShouldBe("Date of birth is required.");
    }

    [Fact]
    public async Task MismatchingPasswordsCreateNobody()
    {
        var created = false;
        var viewModel = Filled(NewFirstUser(() => created = true));
        viewModel.ConfirmPassword = "something else entirely";

        await viewModel.CreateCommand.ExecuteAsync(null);

        created.ShouldBeFalse();
        viewModel.ErrorMessage.ShouldBe("The passwords do not match.");
        (await _fixture.List.HandleAsync(new Features.Users.ListUsers.Query(), Ct)).TotalCount.ShouldBe(0);
    }

    [Fact]
    public async Task AValidFirstUserIsCreatedAndThePasswordsAreCleared()
    {
        var created = false;
        var viewModel = Filled(NewFirstUser(() => created = true));

        await viewModel.CreateCommand.ExecuteAsync(null);

        created.ShouldBeTrue();
        viewModel.Password.ShouldBeEmpty();
        viewModel.ConfirmPassword.ShouldBeEmpty();
        (await _fixture.List.HandleAsync(new Features.Users.ListUsers.Query(), Ct)).TotalCount.ShouldBe(1);
    }

    [Fact]
    public async Task AValidationErrorFromTheHandlerIsShownInTheCurrentLanguage()
    {
        var created = false;
        var viewModel = Filled(NewFirstUser(() => created = true));
        viewModel.Username = string.Empty;

        await viewModel.CreateCommand.ExecuteAsync(null);

        created.ShouldBeFalse();
        viewModel.ErrorMessage.ShouldNotBeNullOrWhiteSpace();
    }

    private static FirstUserViewModel Filled(FirstUserViewModel viewModel)
    {
        viewModel.FirstName = "Ada";
        viewModel.LastName = "Lovelace";
        viewModel.Username = "ada@example.com";
        viewModel.Password = "correct horse battery";
        viewModel.ConfirmPassword = "correct horse battery";
        viewModel.DateOfBirth = new DateTime(1990, 12, 10);
        return viewModel;
    }
}
