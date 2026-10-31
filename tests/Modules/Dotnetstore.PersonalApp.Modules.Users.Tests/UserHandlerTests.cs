using Dotnetstore.PersonalApp.Shared.Contracts.Events;
using Dotnetstore.PersonalApp.Modules.Users.Features.Users;

namespace Dotnetstore.PersonalApp.Modules.Users.Tests;

public sealed class UserHandlerTests : IDisposable
{
    private readonly UsersFixture _fixture = new();

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public void Dispose() => _fixture.Dispose();

    [Fact]
    public async Task CreateStoresHashedPasswordAndNormalizedUsername()
    {
        var result = await _fixture.Create.HandleAsync(UsersFixture.ValidCommand("Ada@Example.COM"), Ct);

        Assert.True(result.IsSuccess);
        Assert.Equal("ada@example.com", result.Value.Username);

        await using var db = await _fixture.DbFactory.CreateDbContextAsync(Ct);
        var stored = db.Users.Single();
        Assert.NotEqual("correct horse battery", stored.PasswordHash);
        Assert.NotEmpty(stored.PasswordHash);
    }

    [Theory]
    [InlineData("", "Lovelace", "ada@example.com", "correct horse battery", "first_name_required")]
    [InlineData("Ada", "", "ada@example.com", "correct horse battery", "last_name_required")]
    [InlineData("Ada", "Lovelace", "not-an-email", "correct horse battery", "username_invalid")]
    [InlineData("Ada", "Lovelace", "", "correct horse battery", "username_required")]
    [InlineData("Ada", "Lovelace", "ada@example.com", "short", "password_too_short")]
    [InlineData("Ada", "Lovelace", "ada@example.com", "", "password_required")]
    public async Task CreateRejectsInvalidInput(string first, string last, string username, string password, string expectedCode)
    {
        var result = await _fixture.Create.HandleAsync(new CreateUser.Command(first, last, username, password, new DateOnly(1990, 1, 1)), Ct);

        Assert.False(result.IsSuccess);
        Assert.Equal(expectedCode, result.Error.Code);
    }

    [Fact]
    public async Task CreateRejectsFutureDateOfBirth()
    {
        var command = UsersFixture.ValidCommand() with { DateOfBirth = new DateOnly(2027, 1, 1) };

        var result = await _fixture.Create.HandleAsync(command, Ct);

        Assert.Equal("date_of_birth_future", result.Error.Code);
    }

    [Fact]
    public async Task CreateRejectsDuplicateUsername()
    {
        await _fixture.Create.HandleAsync(UsersFixture.ValidCommand(), Ct);

        var result = await _fixture.Create.HandleAsync(UsersFixture.ValidCommand("ADA@example.com"), Ct);

        Assert.Equal("user_conflict", result.Error.Code);
    }

    [Fact]
    public async Task GetReturnsNotFoundForUnknownId()
    {
        var result = await _fixture.Get.HandleAsync(new GetUser.Query(Guid.NewGuid()), Ct);

        Assert.Equal("user_not_found", result.Error.Code);
    }

    [Fact]
    public async Task GetReturnsCreatedUser()
    {
        var created = (await _fixture.Create.HandleAsync(UsersFixture.ValidCommand(), Ct)).Value;

        var result = await _fixture.Get.HandleAsync(new GetUser.Query(created.Id), Ct);

        Assert.Equal("Ada Lovelace", result.Value.FullName);
    }

    [Fact]
    public async Task ListFiltersBySearchAndOrdersByLastName()
    {
        await _fixture.Create.HandleAsync(UsersFixture.ValidCommand("ada@example.com"), Ct);
        await _fixture.Create.HandleAsync(new CreateUser.Command("Alan", "Turing", "alan@example.com", "correct horse battery", new DateOnly(1912, 6, 23)), Ct);

        var all = await _fixture.List.HandleAsync(new ListUsers.Query(), Ct);
        var filtered = await _fixture.List.HandleAsync(new ListUsers.Query("turing"), Ct);

        Assert.Equal(["Lovelace", "Turing"], all.Items.Select(u => u.LastName));
        Assert.Equal(1, filtered.TotalCount);
        Assert.Equal("Alan", filtered.Items[0].FirstName);
    }

    [Fact]
    public async Task ListCapsPageSize()
    {
        var response = await _fixture.List.HandleAsync(new ListUsers.Query(PageSize: 10_000), Ct);

        Assert.Empty(response.Items);
        Assert.Equal(0, response.TotalCount);
    }

    [Fact]
    public async Task UpdateChangesFieldsAndKeepsPasswordWhenNoneGiven()
    {
        var created = (await _fixture.Create.HandleAsync(UsersFixture.ValidCommand(), Ct)).Value;
        string originalHash;
        await using (var db = await _fixture.DbFactory.CreateDbContextAsync(Ct))
        {
            originalHash = db.Users.Single().PasswordHash;
        }

        var result = await _fixture.Update.HandleAsync(
            new UpdateUser.Command(created.Id, "Augusta", "King", "augusta@example.com", null, new DateOnly(1950, 12, 10)), Ct);

        Assert.True(result.IsSuccess);
        Assert.Equal("Augusta King", result.Value.FullName);
        await using var verify = await _fixture.DbFactory.CreateDbContextAsync(Ct);
        Assert.Equal(originalHash, verify.Users.Single().PasswordHash);
    }

    [Fact]
    public async Task UpdateChangesPasswordHashWhenNewPasswordGiven()
    {
        var created = (await _fixture.Create.HandleAsync(UsersFixture.ValidCommand(), Ct)).Value;
        string originalHash;
        await using (var db = await _fixture.DbFactory.CreateDbContextAsync(Ct))
        {
            originalHash = db.Users.Single().PasswordHash;
        }

        await _fixture.Update.HandleAsync(
            new UpdateUser.Command(created.Id, "Ada", "Lovelace", "ada@example.com", "a brand new password", new DateOnly(1990, 12, 10)), Ct);

        await using var verify = await _fixture.DbFactory.CreateDbContextAsync(Ct);
        Assert.NotEqual(originalHash, verify.Users.Single().PasswordHash);
    }

    [Fact]
    public async Task UpdateRejectsUsernameTakenByAnotherUser()
    {
        await _fixture.Create.HandleAsync(UsersFixture.ValidCommand("ada@example.com"), Ct);
        var other = (await _fixture.Create.HandleAsync(UsersFixture.ValidCommand("alan@example.com"), Ct)).Value;

        var result = await _fixture.Update.HandleAsync(
            new UpdateUser.Command(other.Id, "Alan", "Turing", "ada@example.com", null, new DateOnly(1912, 6, 23)), Ct);

        Assert.Equal("user_conflict", result.Error.Code);
    }

    [Fact]
    public async Task UpdateReturnsNotFoundForUnknownId()
    {
        var result = await _fixture.Update.HandleAsync(
            new UpdateUser.Command(Guid.NewGuid(), "Ada", "Lovelace", "ada@example.com", null, new DateOnly(1990, 1, 1)), Ct);

        Assert.Equal("user_not_found", result.Error.Code);
    }

    [Fact]
    public async Task DeleteRemovesUserAndReportsNotFoundTheSecondTime()
    {
        var created = (await _fixture.Create.HandleAsync(UsersFixture.ValidCommand(), Ct)).Value;

        var first = await _fixture.Delete.HandleAsync(new DeleteUser.Command(created.Id), Ct);
        var second = await _fixture.Delete.HandleAsync(new DeleteUser.Command(created.Id), Ct);

        Assert.True(first.IsSuccess);
        Assert.Equal("user_not_found", second.Error.Code);
    }

    [Fact]
    public async Task DeletePublishesUserDeletedOnlyWhenAUserWasRemoved()
    {
        var created = (await _fixture.Create.HandleAsync(UsersFixture.ValidCommand(), Ct)).Value;

        await _fixture.Delete.HandleAsync(new DeleteUser.Command(created.Id), Ct);
        await _fixture.Delete.HandleAsync(new DeleteUser.Command(created.Id), Ct);

        var published = Assert.Single(_fixture.Events.Published);
        Assert.Equal(new UserDeleted(created.Id), published);
    }
}
