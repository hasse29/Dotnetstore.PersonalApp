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

        result.IsSuccess.ShouldBeTrue();
        result.Value.FullName.ShouldBe("Anna Svensson");
        result.Value.Initials.ShouldBe("AS");
        result.Value.JobTitle.ShouldBeNull();
        result.Value.ClientId.ShouldBe(clientId);
    }

    [Fact]
    public async Task CreateRejectsAnUnknownClient()
    {
        var result = await _fixture.CreateContactPerson.HandleAsync(
            new CreateContactPerson.Command(Guid.NewGuid(), "Anna", "Svensson", null, null), Ct);

        result.Error.Code.ShouldBe("client_not_found");
    }

    [Theory]
    [InlineData("", "Svensson", "contact_person_first_name_required")]
    [InlineData("Anna", " ", "contact_person_last_name_required")]
    public async Task CreateRejectsMissingNames(string first, string last, string expectedCode)
    {
        var clientId = await CreateClientAsync();

        var result = await _fixture.CreateContactPerson.HandleAsync(new CreateContactPerson.Command(clientId, first, last, null, null), Ct);

        result.Error.Code.ShouldBe(expectedCode);
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

        people.Select(p => p.FullName).ShouldBe(["Zoe Berg", "Anna Åberg"]);
    }

    [Fact]
    public async Task UpdateChangesThePerson()
    {
        var clientId = await CreateClientAsync();
        var person = (await _fixture.CreateContactPerson.HandleAsync(new CreateContactPerson.Command(clientId, "Anna", "Svensson", null, null), Ct)).Value;

        var result = await _fixture.UpdateContactPerson.HandleAsync(new UpdateContactPerson.Command(person.Id, "Anna", "Berg", "CFO", "Prefers email"), Ct);

        result.IsSuccess.ShouldBeTrue();
        result.Value.LastName.ShouldBe("Berg");
        result.Value.JobTitle.ShouldBe("CFO");
    }

    [Fact]
    public async Task UpdateReportsAMissingPerson()
    {
        var result = await _fixture.UpdateContactPerson.HandleAsync(new UpdateContactPerson.Command(Guid.NewGuid(), "A", "B", null, null), Ct);

        result.Error.Code.ShouldBe("contact_person_not_found");
    }

    [Fact]
    public async Task DeleteRemovesThePersonAndPublishesTheEvent()
    {
        var clientId = await CreateClientAsync();
        var person = (await _fixture.CreateContactPerson.HandleAsync(new CreateContactPerson.Command(clientId, "Anna", "Svensson", null, null), Ct)).Value;

        var result = await _fixture.DeleteContactPerson.HandleAsync(new DeleteContactPerson.Command(person.Id), Ct);
        var again = await _fixture.DeleteContactPerson.HandleAsync(new DeleteContactPerson.Command(person.Id), Ct);

        result.IsSuccess.ShouldBeTrue();
        again.Error.Code.ShouldBe("contact_person_not_found");
        _fixture.Events.Published.ShouldHaveSingleItem().ShouldBe(new ContactPersonDeleted(person.Id));
        (await _fixture.ListContactPersons.HandleAsync(new ListContactPersons.Query(clientId), Ct)).ShouldBeEmpty();
    }

    [Fact]
    public async Task DeletingTheClientRemovesItsPeopleAndTellsTheOtherModules()
    {
        var clientId = await CreateClientAsync();
        var anna = (await _fixture.CreateContactPerson.HandleAsync(new CreateContactPerson.Command(clientId, "Anna", "Svensson", null, null), Ct)).Value;
        var bo = (await _fixture.CreateContactPerson.HandleAsync(new CreateContactPerson.Command(clientId, "Bo", "Berg", null, null), Ct)).Value;

        await _fixture.Delete.HandleAsync(new DeleteClient.Command(clientId), Ct);

        (await _fixture.ListContactPersons.HandleAsync(new ListContactPersons.Query(clientId), Ct)).ShouldBeEmpty();

        // Each person is announced before the client itself, so consumers never see a client without its people gone.
        _fixture.Events.Published[^1].ShouldBe(new ClientDeleted(clientId));
        _fixture.Events.Published[..^1].ShouldBe(new object[] { new ContactPersonDeleted(anna.Id), new ContactPersonDeleted(bo.Id) }, ignoreOrder: true);
    }
}
