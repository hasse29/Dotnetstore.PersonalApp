using Dotnetstore.PersonalApp.Shared.Contracts.Events;
using Dotnetstore.PersonalApp.Modules.Clients.Features.Clients;

namespace Dotnetstore.PersonalApp.Modules.Clients.Tests;

public sealed class ClientHandlerTests : IDisposable
{
    private readonly ClientsFixture _fixture = new();

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public void Dispose() => _fixture.Dispose();

    [Fact]
    public async Task CreateStoresTheCompanyWithNormalizedFields()
    {
        var command = ClientsFixture.ValidCommand("556677 8899") with { Name = "  Acme AB  ", Notes = "   ", Phone = "" };

        var result = await _fixture.Create.HandleAsync(command, Ct);

        Assert.True(result.IsSuccess);
        Assert.Equal("Acme AB", result.Value.Name);
        Assert.Equal("5566778899", result.Value.OrganizationNumber);
        Assert.Null(result.Value.Notes);
        Assert.Null(result.Value.Phone);
        Assert.Equal(new DateTimeOffset(2026, 10, 31, 12, 0, 0, TimeSpan.Zero), result.Value.CreatedAt);
    }

    [Fact]
    public async Task CreateAcceptsOnlyTheRequiredFields()
    {
        var result = await _fixture.Create.HandleAsync(new CreateClient.Command("Acme AB", "556677-8899", null, null, null, null, null), Ct);

        Assert.True(result.IsSuccess);
        Assert.Null(result.Value.Email);
        Assert.Null(result.Value.Website);
    }

    [Theory]
    [InlineData("", "556677-8899", null, null, "client_name_required")]
    [InlineData("Acme", "", null, null, "client_organization_number_required")]
    [InlineData("Acme", "55_66!", null, null, "client_organization_number_invalid")]
    [InlineData("Acme", "556677-8899", "not-an-email", null, "client_email_invalid")]
    [InlineData("Acme", "556677-8899", null, "ftp://acme.example", "client_website_invalid")]
    [InlineData("Acme", "556677-8899", null, "acme.example", "client_website_invalid")]
    public async Task CreateRejectsInvalidInput(string name, string organizationNumber, string? email, string? website, string expectedCode)
    {
        var result = await _fixture.Create.HandleAsync(new CreateClient.Command(name, organizationNumber, null, email, null, website, null), Ct);

        Assert.False(result.IsSuccess);
        Assert.Equal(expectedCode, result.Error.Code);
    }

    [Fact]
    public async Task CreateRejectsTooLongValues()
    {
        var result = await _fixture.Create.HandleAsync(ClientsFixture.ValidCommand(name: new string('x', 201)), Ct);

        Assert.Equal("client_name_too_long", result.Error.Code);
    }

    [Fact]
    public async Task CreateRejectsDuplicateOrganizationNumberIgnoringSpacesAndCase()
    {
        await _fixture.Create.HandleAsync(ClientsFixture.ValidCommand("AB-123"), Ct);

        var result = await _fixture.Create.HandleAsync(ClientsFixture.ValidCommand("ab- 123", "Other AB"), Ct);

        Assert.Equal("client_conflict", result.Error.Code);
    }

    [Fact]
    public async Task GetReturnsTheClientOrNotFound()
    {
        var created = (await _fixture.Create.HandleAsync(ClientsFixture.ValidCommand(), Ct)).Value;

        var found = await _fixture.Get.HandleAsync(new GetClient.Query(created.Id), Ct);
        var missing = await _fixture.Get.HandleAsync(new GetClient.Query(Guid.NewGuid()), Ct);

        Assert.Equal("Acme AB", found.Value.Name);
        Assert.Equal("client_not_found", missing.Error.Code);
    }

    [Fact]
    public async Task ListSearchesNameOrganizationNumberAndEmailOrderedByName()
    {
        await _fixture.Create.HandleAsync(ClientsFixture.ValidCommand("111", "Zeta AB"), Ct);
        await _fixture.Create.HandleAsync(new CreateClient.Command("Alpha AB", "222", null, "sales@alpha.example", null, null, null), Ct);

        var all = await _fixture.List.HandleAsync(new ListClients.Query(), Ct);
        var byName = await _fixture.List.HandleAsync(new ListClients.Query("zeta"), Ct);
        var byOrganization = await _fixture.List.HandleAsync(new ListClients.Query("222"), Ct);
        var byEmail = await _fixture.List.HandleAsync(new ListClients.Query("alpha.example"), Ct);

        Assert.Equal(["Alpha AB", "Zeta AB"], all.Items.Select(c => c.Name));
        Assert.Equal("Zeta AB", Assert.Single(byName.Items).Name);
        Assert.Equal("Alpha AB", Assert.Single(byOrganization.Items).Name);
        Assert.Equal("Alpha AB", Assert.Single(byEmail.Items).Name);
    }

    [Fact]
    public async Task ListCapsPageSize()
    {
        for (var i = 0; i < 3; i++)
        {
            await _fixture.Create.HandleAsync(ClientsFixture.ValidCommand($"ORG-{i}", $"Client {i}"), Ct);
        }

        var response = await _fixture.List.HandleAsync(new ListClients.Query(PageSize: 2), Ct);
        var huge = await _fixture.List.HandleAsync(new ListClients.Query(PageSize: 10_000), Ct);

        Assert.Equal(2, response.Items.Count);
        Assert.Equal(3, response.TotalCount);
        Assert.Equal(3, huge.Items.Count);
    }

    [Fact]
    public async Task UpdateChangesFieldsAndClearsOptionalOnes()
    {
        var created = (await _fixture.Create.HandleAsync(ClientsFixture.ValidCommand(), Ct)).Value;

        var result = await _fixture.Update.HandleAsync(
            new UpdateClient.Command(created.Id, "Acme Group AB", "556677-8899", null, "", "", "https://acme.example", null), Ct);

        Assert.True(result.IsSuccess);
        Assert.Equal("Acme Group AB", result.Value.Name);
        Assert.Null(result.Value.VatNumber);
        Assert.Null(result.Value.Email);
        Assert.Equal("https://acme.example", result.Value.Website);
    }

    [Fact]
    public async Task UpdateRejectsOrganizationNumberOfAnotherClientButAllowsKeepingItsOwn()
    {
        await _fixture.Create.HandleAsync(ClientsFixture.ValidCommand("111", "One AB"), Ct);
        var other = (await _fixture.Create.HandleAsync(ClientsFixture.ValidCommand("222", "Two AB"), Ct)).Value;

        var clash = await _fixture.Update.HandleAsync(
            new UpdateClient.Command(other.Id, "Two AB", "111", null, null, null, null, null), Ct);
        var own = await _fixture.Update.HandleAsync(
            new UpdateClient.Command(other.Id, "Two Renamed AB", "222", null, null, null, null, null), Ct);

        Assert.Equal("client_conflict", clash.Error.Code);
        Assert.True(own.IsSuccess);
    }

    [Fact]
    public async Task UpdateReturnsNotFoundForUnknownId()
    {
        var result = await _fixture.Update.HandleAsync(
            new UpdateClient.Command(Guid.NewGuid(), "Acme AB", "556677-8899", null, null, null, null, null), Ct);

        Assert.Equal("client_not_found", result.Error.Code);
    }

    [Fact]
    public async Task DeleteRemovesTheClientAndReportsNotFoundTheSecondTime()
    {
        var created = (await _fixture.Create.HandleAsync(ClientsFixture.ValidCommand(), Ct)).Value;

        var first = await _fixture.Delete.HandleAsync(new DeleteClient.Command(created.Id), Ct);
        var second = await _fixture.Delete.HandleAsync(new DeleteClient.Command(created.Id), Ct);

        Assert.True(first.IsSuccess);
        Assert.Equal("client_not_found", second.Error.Code);
    }

    [Fact]
    public async Task DeletePublishesClientDeletedOnlyWhenAClientWasRemoved()
    {
        var created = (await _fixture.Create.HandleAsync(ClientsFixture.ValidCommand(), Ct)).Value;

        await _fixture.Delete.HandleAsync(new DeleteClient.Command(created.Id), Ct);
        await _fixture.Delete.HandleAsync(new DeleteClient.Command(created.Id), Ct);

        var published = Assert.Single(_fixture.Events.Published);
        Assert.Equal(new ClientDeleted(created.Id), published);
    }
}
