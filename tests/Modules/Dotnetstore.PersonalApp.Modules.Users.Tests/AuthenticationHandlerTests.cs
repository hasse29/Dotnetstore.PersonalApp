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
        Assert.False(await _hasUsers.HandleAsync(Ct));

        await _fixture.Create.HandleAsync(UsersFixture.ValidCommand(), Ct);

        Assert.True(await _hasUsers.HandleAsync(Ct));
    }

    [Fact]
    public async Task LoginSucceedsWithCorrectCredentialsIgnoringUsernameCase()
    {
        await _fixture.Create.HandleAsync(UsersFixture.ValidCommand(), Ct);

        var result = await _login.HandleAsync(new Login.Command(" ADA@example.com ", "correct horse battery"), Ct);

        Assert.True(result.IsSuccess);
        Assert.Equal("ada@example.com", result.Value.Username);
        Assert.Equal("Ada Lovelace", result.Value.FullName);
    }

    [Fact]
    public async Task LoginFailsWithWrongPassword()
    {
        await _fixture.Create.HandleAsync(UsersFixture.ValidCommand(), Ct);

        var result = await _login.HandleAsync(new Login.Command("ada@example.com", "wrong password"), Ct);

        Assert.Equal("invalid_credentials", result.Error.Code);
    }

    [Fact]
    public async Task LoginGivesSameErrorForUnknownUserAsForWrongPassword()
    {
        await _fixture.Create.HandleAsync(UsersFixture.ValidCommand(), Ct);

        var unknown = await _login.HandleAsync(new Login.Command("nobody@example.com", "correct horse battery"), Ct);
        var wrong = await _login.HandleAsync(new Login.Command("ada@example.com", "wrong password"), Ct);

        Assert.Equal(wrong.Error, unknown.Error);
    }

    [Theory]
    [InlineData("", "password")]
    [InlineData("ada@example.com", "")]
    public async Task LoginRequiresUsernameAndPassword(string username, string password)
    {
        var result = await _login.HandleAsync(new Login.Command(username, password), Ct);

        Assert.Equal("credentials_required", result.Error.Code);
    }
}
