using Dotnetstore.PersonalApp.Modules.Clients.Features.ContactPersons;

namespace Dotnetstore.PersonalApp.Modules.Clients.Tests;

public sealed class ContactPersonLookupTests : IDisposable
{
    private readonly ClientsFixture _fixture = new();

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public void Dispose() => _fixture.Dispose();

    [Fact]
    public async Task ListsTheClientsPeopleByNameWithTheirIds()
    {
        var clientId = (await _fixture.Create.HandleAsync(ClientsFixture.ValidCommand(), Ct)).Value.Id;
        var zoe = (await _fixture.CreateContactPerson.HandleAsync(new CreateContactPerson.Command(clientId, "Zoe", "Berg", null, null), Ct)).Value;
        var anna = (await _fixture.CreateContactPerson.HandleAsync(new CreateContactPerson.Command(clientId, "Anna", "Andersson", null, null), Ct)).Value;

        var people = await new ContactPersonLookup(_fixture.ListContactPersons).ListAsync(clientId, Ct);

        people.Select(p => p.Id).ShouldBe([anna.Id, zoe.Id]);
        people.Select(p => p.Name).ShouldBe(["Anna Andersson", "Zoe Berg"]);
    }
}
