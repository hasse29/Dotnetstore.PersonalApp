using Dotnetstore.PersonalApp.Modules.Addresses.Features.AddressTypes;

namespace Dotnetstore.PersonalApp.Modules.Addresses.Tests;

public sealed class AddressTypeHandlerTests : IDisposable
{
    private readonly AddressesFixture _fixture = new();

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public void Dispose() => _fixture.Dispose();

    [Fact]
    public async Task DefaultTypesAreSeeded()
    {
        var all = await _fixture.List.HandleAsync(new ListAddressTypes.Query(), Ct);

        Assert.Equal(["Billing", "Delivery", "Home", "Visiting"], all.Items.Select(t => t.Name));
    }

    [Fact]
    public async Task CreateStoresTheTypeWithNormalizedFields()
    {
        var result = await _fixture.Create.HandleAsync(new CreateAddressType.Command("  Postal  ", "   "), Ct);

        Assert.True(result.IsSuccess);
        Assert.Equal("Postal", result.Value.Name);
        Assert.Null(result.Value.Description);
        Assert.Equal(new DateTimeOffset(2026, 10, 31, 12, 0, 0, TimeSpan.Zero), result.Value.CreatedAt);
    }

    [Theory]
    [InlineData("", "address_type_name_required")]
    [InlineData("   ", "address_type_name_required")]
    public async Task CreateRejectsInvalidInput(string name, string expectedCode)
    {
        var result = await _fixture.Create.HandleAsync(new CreateAddressType.Command(name, null), Ct);

        Assert.False(result.IsSuccess);
        Assert.Equal(expectedCode, result.Error.Code);
    }

    [Fact]
    public async Task CreateRejectsTooLongValues()
    {
        var name = await _fixture.Create.HandleAsync(new CreateAddressType.Command(new string('x', 101), null), Ct);
        var description = await _fixture.Create.HandleAsync(new CreateAddressType.Command("Long", new string('x', 501)), Ct);

        Assert.Equal("address_type_name_too_long", name.Error.Code);
        Assert.Equal("address_type_description_too_long", description.Error.Code);
    }

    [Fact]
    public async Task CreateRejectsDuplicateNameIgnoringCaseAndSurroundingSpaces()
    {
        var result = await _fixture.Create.HandleAsync(new CreateAddressType.Command("  bILLING ", null), Ct);

        Assert.Equal("address_type_conflict", result.Error.Code);
    }

    [Fact]
    public async Task GetReturnsTheTypeOrNotFound()
    {
        var created = (await _fixture.Create.HandleAsync(new CreateAddressType.Command("Postal", "PO box"), Ct)).Value;

        var found = await _fixture.Get.HandleAsync(new GetAddressType.Query(created.Id), Ct);
        var missing = await _fixture.Get.HandleAsync(new GetAddressType.Query(Guid.NewGuid()), Ct);

        Assert.Equal("PO box", found.Value.Description);
        Assert.Equal("address_type_not_found", missing.Error.Code);
    }

    [Fact]
    public async Task ListSearchesNameAndDescription()
    {
        var byName = await _fixture.List.HandleAsync(new ListAddressTypes.Query("deliv"), Ct);
        var byDescription = await _fixture.List.HandleAsync(new ListAddressTypes.Query("invoices"), Ct);

        Assert.Equal("Delivery", Assert.Single(byName.Items).Name);
        Assert.Equal("Billing", Assert.Single(byDescription.Items).Name);
    }

    [Fact]
    public async Task ListCapsPageSize()
    {
        var page = await _fixture.List.HandleAsync(new ListAddressTypes.Query(PageSize: 2), Ct);
        var huge = await _fixture.List.HandleAsync(new ListAddressTypes.Query(PageSize: 10_000), Ct);

        Assert.Equal(2, page.Items.Count);
        Assert.Equal(4, page.TotalCount);
        Assert.Equal(4, huge.Items.Count);
    }

    [Fact]
    public async Task UpdateChangesFieldsAndClearsTheDescription()
    {
        var created = (await _fixture.Create.HandleAsync(new CreateAddressType.Command("Postal", "PO box"), Ct)).Value;

        var result = await _fixture.Update.HandleAsync(new UpdateAddressType.Command(created.Id, "Mailing", ""), Ct);

        Assert.True(result.IsSuccess);
        Assert.Equal("Mailing", result.Value.Name);
        Assert.Null(result.Value.Description);
    }

    [Fact]
    public async Task UpdateRejectsNameOfAnotherTypeButAllowsChangingCaseOfItsOwn()
    {
        var created = (await _fixture.Create.HandleAsync(new CreateAddressType.Command("Postal", null), Ct)).Value;

        var clash = await _fixture.Update.HandleAsync(new UpdateAddressType.Command(created.Id, "billing", null), Ct);
        var own = await _fixture.Update.HandleAsync(new UpdateAddressType.Command(created.Id, "POSTAL", null), Ct);

        Assert.Equal("address_type_conflict", clash.Error.Code);
        Assert.True(own.IsSuccess);
    }

    [Fact]
    public async Task UpdateReturnsNotFoundForUnknownId()
    {
        var result = await _fixture.Update.HandleAsync(new UpdateAddressType.Command(Guid.NewGuid(), "Postal", null), Ct);

        Assert.Equal("address_type_not_found", result.Error.Code);
    }

    [Fact]
    public async Task DeleteRemovesTheTypeAndReportsNotFoundTheSecondTime()
    {
        var created = (await _fixture.Create.HandleAsync(new CreateAddressType.Command("Postal", null), Ct)).Value;

        var first = await _fixture.Delete.HandleAsync(new DeleteAddressType.Command(created.Id), Ct);
        var second = await _fixture.Delete.HandleAsync(new DeleteAddressType.Command(created.Id), Ct);

        Assert.True(first.IsSuccess);
        Assert.Equal("address_type_not_found", second.Error.Code);
    }
}
