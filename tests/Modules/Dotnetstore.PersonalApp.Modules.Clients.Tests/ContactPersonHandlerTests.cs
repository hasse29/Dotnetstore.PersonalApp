using Dotnetstore.PersonalApp.Modules.Clients.Features.Clients;
using Dotnetstore.PersonalApp.Modules.Clients.Features.ContactPersons;
using Dotnetstore.PersonalApp.Shared.Contracts.Events;

namespace Dotnetstore.PersonalApp.Modules.Clients.Tests;

public sealed class ContactPersonHandlerTests : IDisposable
{
    private readonly ClientsFixture _fixture = new();

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public void Dispose() => _fixture.Dispose();

    private async Task<Guid> CreateClientAsync(string organizationNumber = "556677-8899") =>
        (await _fixture.Create.HandleAsync(ClientsFixture.ValidCommand(organizationNumber), Ct)).Value.Id;

    [Fact]
    public async Task CreateStoresTheTrimmedPersonForTheClient()
    {
        var clientId = await CreateClientAsync();

        var result = await _fixture.CreateContactPerson.HandleAsync(
            new CreateContactPerson.Command(clientId, "  Anna ", " Svensson ", "  ", null), Ct);

        Assert.True(result.IsSuccess);
        Assert.Equal("Anna Svensson", result.Value.FullName);
        Assert.Equal("AS", result.Value.Initials);
        Assert.Null(result.Value.JobTitle);
        Assert.Equal(clientId, result.Value.ClientId);
    }

    [Fact]
    public async Task CreateRejectsAnUnknownClient()
    {
        var result = await _fixture.CreateContactPerson.HandleAsync(
            new CreateContactPerson.Command(Guid.NewGuid(), "Anna", "Svensson", null, null), Ct);

        Assert.Equal("client_not_found", result.Error.Code);
    }

    [Theory]
    [InlineData("", "Svensson", "contact_person_first_name_required")]
    [InlineData("Anna", " ", "contact_person_last_name_required")]
    public async Task CreateRejectsMissingNames(string first, string last, string expectedCode)
    {
        var clientId = await CreateClientAsync();

        var result = await _fixture.CreateContactPerson.HandleAsync(new CreateContactPerson.Command(clientId, first, last, null, null), Ct);

        Assert.Equal(expectedCode, result.Error.Code);
    }

    [Fact]
    public async Task ListIsScopedToTheClientAndSortedByName()
    {
        var acme = await CreateClientAsync();
        var other = await CreateClientAsync("111111-2222");
        await _fixture.CreateContactPerson.HandleAsync(new CreateContactPerson.Command(acme, "Zoe", "Berg", null, null), Ct);
        await _fixture.CreateContactPerson.HandleAsync(new CreateContactPerson.Command(acme, "Anna", "Åberg", null, null), Ct);
        await _fixture.CreateContactPerson.HandleAsync(new CreateContactPerson.Command(other, "Olle", "Other", null, null), Ct);

        var people = await _fixture.ListContactPersons.HandleAsync(new ListContactPersons.Query(acme), Ct);

        Assert.Equal(["Zoe Berg", "Anna Åberg"], people.Select(p => p.FullName));
    }

    [Fact]
    public async Task UpdateChangesThePerson()
    {
        var clientId = await CreateClientAsync();
        var person = (await _fixture.CreateContactPerson.HandleAsync(new CreateContactPerson.Command(clientId, "Anna", "Svensson", null, null), Ct)).Value;

        var result = await _fixture.UpdateContactPerson.HandleAsync(new UpdateContactPerson.Command(person.Id, "Anna", "Berg", "CFO", "Prefers email"), Ct);

        Assert.True(result.IsSuccess);
        Assert.Equal("Berg", result.Value.LastName);
        Assert.Equal("CFO", result.Value.JobTitle);
    }

    [Fact]
    public async Task UpdateReportsAMissingPerson()
    {
        var result = await _fixture.UpdateContactPerson.HandleAsync(new UpdateContactPerson.Command(Guid.NewGuid(), "A", "B", null, null), Ct);

        Assert.Equal("contact_person_not_found", result.Error.Code);
    }

    [Fact]
    public async Task DeleteRemovesThePersonAndPublishesTheEvent()
    {
        var clientId = await CreateClientAsync();
        var person = (await _fixture.CreateContactPerson.HandleAsync(new CreateContactPerson.Command(clientId, "Anna", "Svensson", null, null), Ct)).Value;

        var result = await _fixture.DeleteContactPerson.HandleAsync(new DeleteContactPerson.Command(person.Id), Ct);
        var again = await _fixture.DeleteContactPerson.HandleAsync(new DeleteContactPerson.Command(person.Id), Ct);

        Assert.True(result.IsSuccess);
        Assert.Equal("contact_person_not_found", again.Error.Code);
        Assert.Equal(new ContactPersonDeleted(person.Id), Assert.Single(_fixture.Events.Published));
        Assert.Empty(await _fixture.ListContactPersons.HandleAsync(new ListContactPersons.Query(clientId), Ct));
    }

    [Fact]
    public async Task DeletingTheClientRemovesItsPeopleAndTellsTheOtherModules()
    {
        var clientId = await CreateClientAsync();
        var anna = (await _fixture.CreateContactPerson.HandleAsync(new CreateContactPerson.Command(clientId, "Anna", "Svensson", null, null), Ct)).Value;
        var bo = (await _fixture.CreateContactPerson.HandleAsync(new CreateContactPerson.Command(clientId, "Bo", "Berg", null, null), Ct)).Value;

        await _fixture.Delete.HandleAsync(new DeleteClient.Command(clientId), Ct);

        Assert.Empty(await _fixture.ListContactPersons.HandleAsync(new ListContactPersons.Query(clientId), Ct));

        // Each person is announced before the client itself, so consumers never see a client without its people gone.
        Assert.Equal(new ClientDeleted(clientId), _fixture.Events.Published[^1]);
        Assert.Equivalent(
            new object[] { new ContactPersonDeleted(anna.Id), new ContactPersonDeleted(bo.Id) },
            _fixture.Events.Published[..^1]);
    }
}
