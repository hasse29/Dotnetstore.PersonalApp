using Dotnetstore.PersonalApp.Modules.Addresses.Features.Addresses;
using Dotnetstore.PersonalApp.Modules.Addresses.Features.AddressTypes;

namespace Dotnetstore.PersonalApp.Modules.Addresses.Tests;

public sealed class AddressHandlerTests : IDisposable
{
    private readonly AddressesFixture _fixture = new();
    private readonly Guid _owner = Guid.NewGuid();

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public void Dispose() => _fixture.Dispose();

    [Fact]
    public async Task CreateStoresTheAddressWithNormalizedFields()
    {
        var command = _fixture.ValidAddress(_owner) with { CareOf = "  ", Street1 = "  Drottninggatan 1 ", State = "" };

        var result = await _fixture.CreateAddress.HandleAsync(command, Ct);

        Assert.True(result.IsSuccess);
        Assert.Equal(_owner, result.Value.ObjectId);
        Assert.Equal("Billing", result.Value.AddressTypeName);
        Assert.Equal("Drottninggatan 1", result.Value.Street1);
        Assert.Null(result.Value.CareOf);
        Assert.Null(result.Value.State);
    }

    [Theory]
    [InlineData("", "111 51", "Stockholm", "Sweden", "address_street1_required")]
    [InlineData("Street 1", "", "Stockholm", "Sweden", "address_postal_code_required")]
    [InlineData("Street 1", "111 51", " ", "Sweden", "address_city_required")]
    [InlineData("Street 1", "111 51", "Stockholm", "", "address_country_required")]
    public async Task CreateRequiresStreetPostalCodeCityAndCountry(string street, string postalCode, string city, string country, string expectedCode)
    {
        var command = _fixture.ValidAddress(_owner) with { Street1 = street, PostalCode = postalCode, City = city, Country = country };

        var result = await _fixture.CreateAddress.HandleAsync(command, Ct);

        Assert.Equal(expectedCode, result.Error.Code);
    }

    [Fact]
    public async Task CreateRequiresAnObjectAndAType()
    {
        var noObject = await _fixture.CreateAddress.HandleAsync(_fixture.ValidAddress(Guid.Empty), Ct);
        var noType = await _fixture.CreateAddress.HandleAsync(_fixture.ValidAddress(_owner) with { AddressTypeId = Guid.Empty }, Ct);
        var unknownType = await _fixture.CreateAddress.HandleAsync(_fixture.ValidAddress(_owner, Guid.NewGuid()), Ct);

        Assert.Equal("address_object_required", noObject.Error.Code);
        Assert.Equal("address_type_required", noType.Error.Code);
        Assert.Equal("address_type_not_found", unknownType.Error.Code);
    }

    [Fact]
    public async Task CreateRejectsTooLongValues()
    {
        var result = await _fixture.CreateAddress.HandleAsync(_fixture.ValidAddress(_owner) with { PostalCode = new string('1', 21) }, Ct);

        Assert.Equal("address_postal_code_too_long", result.Error.Code);
    }

    [Fact]
    public async Task ObjectCanHaveSeveralAddressesOfTheSameTypeAndListIsPerObject()
    {
        var other = Guid.NewGuid();
        await _fixture.CreateAddress.HandleAsync(_fixture.ValidAddress(_owner, city: "Uppsala"), Ct);
        await _fixture.CreateAddress.HandleAsync(_fixture.ValidAddress(_owner, city: "Malmö"), Ct);
        await _fixture.CreateAddress.HandleAsync(_fixture.ValidAddress(_owner, _fixture.DeliveryTypeId, "Lund"), Ct);
        await _fixture.CreateAddress.HandleAsync(_fixture.ValidAddress(other), Ct);

        var mine = await _fixture.ListAddresses.HandleAsync(new ListAddresses.Query(_owner), Ct);
        var theirs = await _fixture.ListAddresses.HandleAsync(new ListAddresses.Query(other), Ct);

        Assert.Equal(3, mine.TotalCount);
        Assert.Equal(["Malmö", "Uppsala", "Lund"], mine.Items.Select(a => a.City));
        Assert.Single(theirs.Items);
    }

    [Fact]
    public async Task ListCapsPageSize()
    {
        for (var i = 0; i < 3; i++)
        {
            await _fixture.CreateAddress.HandleAsync(_fixture.ValidAddress(_owner, city: "City " + i), Ct);
        }

        var page = await _fixture.ListAddresses.HandleAsync(new ListAddresses.Query(_owner, PageSize: 2), Ct);
        var huge = await _fixture.ListAddresses.HandleAsync(new ListAddresses.Query(_owner, PageSize: 10_000), Ct);

        Assert.Equal(2, page.Items.Count);
        Assert.Equal(3, page.TotalCount);
        Assert.Equal(3, huge.Items.Count);
    }

    [Fact]
    public async Task GetReturnsTheAddressOrNotFound()
    {
        var created = (await _fixture.CreateAddress.HandleAsync(_fixture.ValidAddress(_owner), Ct)).Value;

        var found = await _fixture.GetAddress.HandleAsync(new GetAddress.Query(created.Id), Ct);
        var missing = await _fixture.GetAddress.HandleAsync(new GetAddress.Query(Guid.NewGuid()), Ct);

        Assert.Equal("Billing", found.Value.AddressTypeName);
        Assert.Equal("address_not_found", missing.Error.Code);
    }

    [Fact]
    public async Task UpdateChangesFieldsAndTypeButNotTheOwner()
    {
        var created = (await _fixture.CreateAddress.HandleAsync(_fixture.ValidAddress(_owner), Ct)).Value;

        var result = await _fixture.UpdateAddress.HandleAsync(
            new UpdateAddress.Command(created.Id, _fixture.DeliveryTypeId, null, "Storgatan 5", null, "222 22", "Lund", "Skåne", "Sweden"), Ct);

        Assert.True(result.IsSuccess);
        Assert.Equal(_owner, result.Value.ObjectId);
        Assert.Equal("Delivery", result.Value.AddressTypeName);
        Assert.Equal("Lund", result.Value.City);
        Assert.Null(result.Value.CareOf);
        Assert.Equal("Skåne", result.Value.State);
    }

    [Fact]
    public async Task UpdateReportsUnknownAddressAndUnknownType()
    {
        var created = (await _fixture.CreateAddress.HandleAsync(_fixture.ValidAddress(_owner), Ct)).Value;

        var unknownAddress = await _fixture.UpdateAddress.HandleAsync(
            new UpdateAddress.Command(Guid.NewGuid(), _fixture.BillingTypeId, null, "S", null, "1", "C", null, "X"), Ct);
        var unknownType = await _fixture.UpdateAddress.HandleAsync(
            new UpdateAddress.Command(created.Id, Guid.NewGuid(), null, "S", null, "1", "C", null, "X"), Ct);

        Assert.Equal("address_not_found", unknownAddress.Error.Code);
        Assert.Equal("address_type_not_found", unknownType.Error.Code);
    }

    [Fact]
    public async Task DeleteRemovesTheAddressAndReportsNotFoundTheSecondTime()
    {
        var created = (await _fixture.CreateAddress.HandleAsync(_fixture.ValidAddress(_owner), Ct)).Value;

        var first = await _fixture.DeleteAddress.HandleAsync(new DeleteAddress.Command(created.Id), Ct);
        var second = await _fixture.DeleteAddress.HandleAsync(new DeleteAddress.Command(created.Id), Ct);

        Assert.True(first.IsSuccess);
        Assert.Equal("address_not_found", second.Error.Code);
    }

    [Fact]
    public async Task DeleteAllRemovesOnlyTheObjectsAddressesAndReportsTheCount()
    {
        var other = Guid.NewGuid();
        await _fixture.CreateAddress.HandleAsync(_fixture.ValidAddress(_owner, city: "Lund"), Ct);
        await _fixture.CreateAddress.HandleAsync(_fixture.ValidAddress(_owner, _fixture.DeliveryTypeId, "Malmö"), Ct);
        await _fixture.CreateAddress.HandleAsync(_fixture.ValidAddress(other), Ct);

        var result = await _fixture.DeleteAddressesForObject.HandleAsync(new DeleteAddressesForObject.Command(_owner), Ct);
        var again = await _fixture.DeleteAddressesForObject.HandleAsync(new DeleteAddressesForObject.Command(_owner), Ct);

        Assert.Equal(2, result.Value);
        Assert.Equal(0, again.Value);
        Assert.Equal(1, (await _fixture.ListAddresses.HandleAsync(new ListAddresses.Query(other), Ct)).TotalCount);
    }

    [Fact]
    public async Task DeleteAllRejectsAnEmptyObjectId()
    {
        var result = await _fixture.DeleteAddressesForObject.HandleAsync(new DeleteAddressesForObject.Command(Guid.Empty), Ct);

        Assert.Equal("address_object_required", result.Error.Code);
    }

    [Fact]
    public async Task AddressTypeInUseCannotBeDeletedUntilItsAddressesAreGone()
    {
        var address = (await _fixture.CreateAddress.HandleAsync(_fixture.ValidAddress(_owner), Ct)).Value;

        var blocked = await _fixture.Delete.HandleAsync(new DeleteAddressType.Command(address.AddressTypeId), Ct);
        await _fixture.DeleteAddress.HandleAsync(new DeleteAddress.Command(address.Id), Ct);
        var allowed = await _fixture.Delete.HandleAsync(new DeleteAddressType.Command(address.AddressTypeId), Ct);

        Assert.Equal("address_type_in_use", blocked.Error.Code);
        Assert.True(allowed.IsSuccess);
    }
}
