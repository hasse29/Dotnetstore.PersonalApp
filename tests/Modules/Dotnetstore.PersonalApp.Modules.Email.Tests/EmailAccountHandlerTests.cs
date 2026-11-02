using Dotnetstore.PersonalApp.Modules.Email.Consumers;
using Dotnetstore.PersonalApp.Modules.Email.Domain;
using Dotnetstore.PersonalApp.Modules.Email.Features.EmailAccounts;
using Dotnetstore.PersonalApp.Shared.Contracts.Events;

namespace Dotnetstore.PersonalApp.Modules.Email.Tests;

public sealed class EmailAccountHandlerTests : IDisposable
{
    private readonly EmailFixture _fixture = new();
    private readonly Guid _user = Guid.NewGuid();

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public void Dispose() => _fixture.Dispose();

    [Fact]
    public async Task CreateStoresATrimmedPersonalAccountWithAnEncryptedPassword()
    {
        var command = EmailFixture.ValidAccount(_user) with { Name = "  Work outgoing ", Host = " smtp.example.com ", Username = " anna " };

        var result = await _fixture.Create.HandleAsync(command, Ct);

        result.IsSuccess.ShouldBeTrue();
        result.Value.IsSystem.ShouldBeFalse();
        result.Value.Name.ShouldBe("Work outgoing");
        result.Value.Host.ShouldBe("smtp.example.com");
        result.Value.Username.ShouldBe("anna");
        result.Value.HasPassword.ShouldBeTrue();
        (await _fixture.StoredPasswordAsync(result.Value.Id)).ShouldBe("protected:s3cret");
    }

    [Fact]
    public async Task CreateWithoutAUsernameKeepsNoPassword()
    {
        var result = await _fixture.Create.HandleAsync(EmailFixture.ValidAccount(_user) with { Username = "  " }, Ct);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Username.ShouldBeNull();
        result.Value.HasPassword.ShouldBeFalse();
    }

    [Fact]
    public async Task CreateCanAddASystemAccount()
    {
        var result = await _fixture.Create.HandleAsync(EmailFixture.ValidAccount(_user, isSystem: true), Ct);

        result.IsSuccess.ShouldBeTrue();
        result.Value.IsSystem.ShouldBeTrue();
    }

    [Theory]
    [InlineData("", "smtp.example.com", 587, "anna@example.com", "email_account_name_required")]
    [InlineData("Work", "", 587, "anna@example.com", "email_account_host_required")]
    [InlineData("Work", "not a host", 587, "anna@example.com", "email_account_host_invalid")]
    [InlineData("Work", "smtp.example.com", 0, "anna@example.com", "email_account_port_invalid")]
    [InlineData("Work", "smtp.example.com", 65536, "anna@example.com", "email_account_port_invalid")]
    [InlineData("Work", "smtp.example.com", 587, "", "email_account_address_required")]
    [InlineData("Work", "smtp.example.com", 587, "not-an-address", "email_account_address_invalid")]
    [InlineData("Work", "smtp.example.com", 587, "Anna <anna@example.com>", "email_account_address_invalid")]
    public async Task CreateRejectsInvalidInput(string name, string host, int port, string address, string expectedCode)
    {
        var command = EmailFixture.ValidAccount(_user) with { Name = name, Host = host, Port = port, EmailAddress = address };

        var result = await _fixture.Create.HandleAsync(command, Ct);

        result.Error.Code.ShouldBe(expectedCode);
    }

    [Fact]
    public async Task CreateRejectsUndefinedEnumsTooLongFieldsAndMissingUser()
    {
        var protocol = await _fixture.Create.HandleAsync(EmailFixture.ValidAccount(_user) with { Protocol = (MailProtocol)42 }, Ct);
        var security = await _fixture.Create.HandleAsync(EmailFixture.ValidAccount(_user) with { Security = (ConnectionSecurity)42 }, Ct);
        var longName = await _fixture.Create.HandleAsync(EmailFixture.ValidAccount(_user, new string('a', 101)), Ct);
        var longPassword = await _fixture.Create.HandleAsync(EmailFixture.ValidAccount(_user) with { Password = new string('a', 501) }, Ct);
        var noUser = await _fixture.Create.HandleAsync(EmailFixture.ValidAccount(Guid.Empty), Ct);

        protocol.Error.Code.ShouldBe("email_account_protocol_invalid");
        security.Error.Code.ShouldBe("email_account_security_invalid");
        longName.Error.Code.ShouldBe("email_account_name_too_long");
        longPassword.Error.Code.ShouldBe("email_account_password_too_long");
        noUser.Error.Code.ShouldBe("email_account_user_required");
    }

    [Fact]
    public async Task UserCanHaveSeveralAccountsButNamesAreUniquePerOwnerIgnoringCase()
    {
        await _fixture.Create.HandleAsync(EmailFixture.ValidAccount(_user, "Work"), Ct);
        var other = await _fixture.Create.HandleAsync(EmailFixture.ValidAccount(_user, "Private"), Ct);
        var duplicate = await _fixture.Create.HandleAsync(EmailFixture.ValidAccount(_user, "work"), Ct);
        var otherUser = await _fixture.Create.HandleAsync(EmailFixture.ValidAccount(Guid.NewGuid(), "Work"), Ct);
        var system = await _fixture.Create.HandleAsync(EmailFixture.ValidAccount(_user, "Work", isSystem: true), Ct);
        var duplicateSystem = await _fixture.Create.HandleAsync(EmailFixture.ValidAccount(_user, "WORK", isSystem: true), Ct);

        other.IsSuccess.ShouldBeTrue();
        duplicate.Error.Code.ShouldBe("email_account_conflict");
        otherUser.IsSuccess.ShouldBeTrue();
        system.IsSuccess.ShouldBeTrue();
        duplicateSystem.Error.Code.ShouldBe("email_account_conflict");
    }

    [Fact]
    public async Task ListReturnsTheUsersOwnAccountsAndTheSystemAccountsButNotOthers()
    {
        await _fixture.Create.HandleAsync(EmailFixture.ValidAccount(_user, "Zebra"), Ct);
        await _fixture.Create.HandleAsync(EmailFixture.ValidAccount(_user, "alpha"), Ct);
        await _fixture.Create.HandleAsync(EmailFixture.ValidAccount(_user, "Shared", isSystem: true), Ct);
        await _fixture.Create.HandleAsync(EmailFixture.ValidAccount(Guid.NewGuid(), "Someone else's"), Ct);

        var response = await _fixture.List.HandleAsync(new ListEmailAccounts.Query(_user), Ct);

        response.TotalCount.ShouldBe(3);
        response.Items.Select(a => a.Name).ShouldBe(["alpha", "Zebra", "Shared"]);
    }

    [Fact]
    public async Task UpdateChangesTheAccountAndKeepsThePasswordWhenNoneIsGiven()
    {
        var created = (await _fixture.Create.HandleAsync(EmailFixture.ValidAccount(_user), Ct)).Value;

        var result = await _fixture.Update.HandleAsync(
            new UpdateEmailAccount.Command(created.Id, _user, "Renamed", MailProtocol.Pop3, "smtp.example.com", 587, ConnectionSecurity.StartTls, "anna@example.com", "anna", null),
            Ct);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Name.ShouldBe("Renamed");
        result.Value.Protocol.ShouldBe(MailProtocol.Pop3);
        result.Value.Port.ShouldBe(587);
        (await _fixture.StoredPasswordAsync(created.Id)).ShouldBe("protected:s3cret");
    }

    [Fact]
    public async Task UpdateReplacesThePasswordWhenOneIsGivenAndClearsItWithoutAUsername()
    {
        var created = (await _fixture.Create.HandleAsync(EmailFixture.ValidAccount(_user), Ct)).Value;
        UpdateEmailAccount.Command Edit(string? username, string? password) =>
            new(created.Id, _user, "Work outgoing", MailProtocol.Smtp, "smtp.example.com", 587, ConnectionSecurity.StartTls, "anna@example.com", username, password);

        await _fixture.Update.HandleAsync(Edit("anna", "new-secret"), Ct);
        var replaced = await _fixture.StoredPasswordAsync(created.Id);
        await _fixture.Update.HandleAsync(Edit(null, "ignored"), Ct);
        var cleared = await _fixture.StoredPasswordAsync(created.Id);

        replaced.ShouldBe("protected:new-secret");
        cleared.ShouldBeNull();
    }

    [Fact]
    public async Task UpdateKeepsNamesUniqueAndHidesOtherUsersAccounts()
    {
        await _fixture.Create.HandleAsync(EmailFixture.ValidAccount(_user, "Work"), Ct);
        var other = (await _fixture.Create.HandleAsync(EmailFixture.ValidAccount(_user, "Private"), Ct)).Value;
        var foreign = (await _fixture.Create.HandleAsync(EmailFixture.ValidAccount(Guid.NewGuid(), "Theirs"), Ct)).Value;
        UpdateEmailAccount.Command Edit(Guid id, string name) =>
            new(id, _user, name, MailProtocol.Smtp, "smtp.example.com", 587, ConnectionSecurity.StartTls, "anna@example.com", null, null);

        var clash = await _fixture.Update.HandleAsync(Edit(other.Id, "work"), Ct);
        var foreignResult = await _fixture.Update.HandleAsync(Edit(foreign.Id, "Mine now"), Ct);

        clash.Error.Code.ShouldBe("email_account_conflict");
        foreignResult.Error.Code.ShouldBe("email_account_not_found");
    }

    [Fact]
    public async Task AnyUserCanChangeAndDeleteASystemAccountButNotAnotherUsersAccount()
    {
        var system = (await _fixture.Create.HandleAsync(EmailFixture.ValidAccount(_user, "Shared", isSystem: true), Ct)).Value;
        var foreign = (await _fixture.Create.HandleAsync(EmailFixture.ValidAccount(Guid.NewGuid(), "Theirs"), Ct)).Value;
        var someoneElse = Guid.NewGuid();

        var visible = await _fixture.Get.HandleAsync(new GetEmailAccount.Query(system.Id, someoneElse), Ct);
        var hidden = await _fixture.Get.HandleAsync(new GetEmailAccount.Query(foreign.Id, _user), Ct);
        var deleteForeign = await _fixture.Delete.HandleAsync(new DeleteEmailAccount.Command(foreign.Id, _user), Ct);
        var deleteSystem = await _fixture.Delete.HandleAsync(new DeleteEmailAccount.Command(system.Id, someoneElse), Ct);

        visible.IsSuccess.ShouldBeTrue();
        hidden.Error.Code.ShouldBe("email_account_not_found");
        deleteForeign.Error.Code.ShouldBe("email_account_not_found");
        deleteSystem.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public async Task DeleteRemovesTheAccount()
    {
        var created = (await _fixture.Create.HandleAsync(EmailFixture.ValidAccount(_user), Ct)).Value;

        var result = await _fixture.Delete.HandleAsync(new DeleteEmailAccount.Command(created.Id, _user), Ct);
        var again = await _fixture.Delete.HandleAsync(new DeleteEmailAccount.Command(created.Id, _user), Ct);

        result.IsSuccess.ShouldBeTrue();
        again.Error.Code.ShouldBe("email_account_not_found");
    }

    [Fact]
    public async Task DeletingAUserRemovesTheirAccountsButNotTheSystemAccountsOrOthers()
    {
        var otherUser = Guid.NewGuid();
        await _fixture.Create.HandleAsync(EmailFixture.ValidAccount(_user, "One"), Ct);
        await _fixture.Create.HandleAsync(EmailFixture.ValidAccount(_user, "Two"), Ct);
        await _fixture.Create.HandleAsync(EmailFixture.ValidAccount(_user, "Shared", isSystem: true), Ct);
        await _fixture.Create.HandleAsync(EmailFixture.ValidAccount(otherUser, "Theirs"), Ct);
        var consumer = new UserDeletedConsumer(_fixture.DeleteForUser);

        await consumer.ConsumeAsync(new UserDeleted(_user), Ct);

        var remaining = await _fixture.List.HandleAsync(new ListEmailAccounts.Query(otherUser), Ct);
        remaining.Items.Select(a => a.Name).ShouldBe(["Theirs", "Shared"]);
    }
}
