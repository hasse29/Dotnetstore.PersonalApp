using Dotnetstore.PersonalApp.Modules.Users.Domain;
using Dotnetstore.PersonalApp.Modules.Users.Features.Authentication;
using Microsoft.AspNetCore.Identity;

namespace Dotnetstore.PersonalApp.Modules.Users.Tests;

public sealed class AuthenticationHandlerTests : IDisposable
{
    private readonly UsersFixture _fixture = new();
    private readonly Login.Handler _login;
    private readonly HasUsers.Handler _hasUsers;

    public AuthenticationHandlerTests()
    {
        _login = new Login.Handler(_fixture.DbFactory, new PasswordHasher<User>());
        _hasUsers = new HasUsers.Handler(_fixture.DbFactory);
    }

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public void Dispose() => _fixture.Dispose();

    [Fact]
    public async Task HasUsersIsFalseUntilAUserIsCreated()
    {
        (await _hasUsers.HandleAsync(Ct)).ShouldBeFalse();

        await _fixture.Create.HandleAsync(UsersFixture.ValidCommand(), Ct);

        (await _hasUsers.HandleAsync(Ct)).ShouldBeTrue();
    }

    [Fact]
    public async Task LoginSucceedsWithCorrectCredentialsIgnoringUsernameCase()
    {
        await _fixture.Create.HandleAsync(UsersFixture.ValidCommand(), Ct);

        var result = await _login.HandleAsync(new Login.Command(" ADA@example.com ", "correct horse battery"), Ct);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Username.ShouldBe("ada@example.com");
        result.Value.FullName.ShouldBe("Ada Lovelace");
    }

    [Fact]
    public async Task LoginFailsWithWrongPassword()
    {
        await _fixture.Create.HandleAsync(UsersFixture.ValidCommand(), Ct);

        var result = await _login.HandleAsync(new Login.Command("ada@example.com", "wrong password"), Ct);

        result.Error.Code.ShouldBe("invalid_credentials");
    }

    [Fact]
    public async Task LoginGivesSameErrorForUnknownUserAsForWrongPassword()
    {
        await _fixture.Create.HandleAsync(UsersFixture.ValidCommand(), Ct);

        var unknown = await _login.HandleAsync(new Login.Command("nobody@example.com", "correct horse battery"), Ct);
        var wrong = await _login.HandleAsync(new Login.Command("ada@example.com", "wrong password"), Ct);

        unknown.Error.ShouldBe(wrong.Error);
    }

    [Theory]
    [InlineData("", "password")]
    [InlineData("ada@example.com", "")]
    public async Task LoginRequiresUsernameAndPassword(string username, string password)
    {
        var result = await _login.HandleAsync(new Login.Command(username, password), Ct);

        result.Error.Code.ShouldBe("credentials_required");
    }
}
