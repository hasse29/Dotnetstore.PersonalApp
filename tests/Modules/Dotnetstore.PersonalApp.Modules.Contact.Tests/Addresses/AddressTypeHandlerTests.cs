using Dotnetstore.PersonalApp.Modules.Contact.Addresses;
using Dotnetstore.PersonalApp.Modules.Contact.Addresses.Features.AddressTypes;

namespace Dotnetstore.PersonalApp.Modules.Contact.Tests.Addresses;

public sealed class AddressTypeHandlerTests : IDisposable
{
    private readonly AddressesFixture _fixture = new();

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public void Dispose() => _fixture.Dispose();

    [Fact]
    public async Task DefaultTypesAreSeeded()
    {
        var all = await _fixture.List.HandleAsync(new ListAddressTypes.Query(), Ct);

        all.Items.Select(t => t.Name).ShouldBe(["Billing", "Delivery", "Home", "Visiting"]);
    }

    [Fact]
    public async Task CreateStoresTheTypeWithNormalizedFields()
    {
        var result = await _fixture.Create.HandleAsync(new CreateAddressType.Command("  Postal  ", "   "), Ct);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Name.ShouldBe("Postal");
        result.Value.Description.ShouldBeNull();
        result.Value.CreatedAt.ShouldBe(new DateTimeOffset(2026, 10, 31, 12, 0, 0, TimeSpan.Zero));
    }

    [Theory]
    [InlineData("", "address_type_name_required")]
    [InlineData("   ", "address_type_name_required")]
    public async Task CreateRejectsInvalidInput(string name, string expectedCode)
    {
        var result = await _fixture.Create.HandleAsync(new CreateAddressType.Command(name, null), Ct);

        result.IsSuccess.ShouldBeFalse();
        result.Error.Code.ShouldBe(expectedCode);
    }

    [Fact]
    public async Task CreateRejectsTooLongValues()
    {
        var name = await _fixture.Create.HandleAsync(new CreateAddressType.Command(new string('x', 101), null), Ct);
        var description = await _fixture.Create.HandleAsync(new CreateAddressType.Command("Long", new string('x', 501)), Ct);

        name.Error.Code.ShouldBe("address_type_name_too_long");
        description.Error.Code.ShouldBe("address_type_description_too_long");
    }

    [Fact]
    public async Task CreateRejectsDuplicateNameIgnoringCaseAndSurroundingSpaces()
    {
        var result = await _fixture.Create.HandleAsync(new CreateAddressType.Command("  bILLING ", null), Ct);

        result.Error.Code.ShouldBe("address_type_conflict");
    }

    [Fact]
    public async Task GetReturnsTheTypeOrNotFound()
    {
        var created = (await _fixture.Create.HandleAsync(new CreateAddressType.Command("Postal", "PO box"), Ct)).Value;

        var found = await _fixture.Get.HandleAsync(new GetAddressType.Query(created.Id), Ct);
        var missing = await _fixture.Get.HandleAsync(new GetAddressType.Query(Guid.NewGuid()), Ct);

        found.Value.Description.ShouldBe("PO box");
        missing.Error.Code.ShouldBe("address_type_not_found");
    }

    [Fact]
    public async Task ListSearchesNameAndDescription()
    {
        var byName = await _fixture.List.HandleAsync(new ListAddressTypes.Query("deliv"), Ct);
        var byDescription = await _fixture.List.HandleAsync(new ListAddressTypes.Query("invoices"), Ct);

        byName.Items.ShouldHaveSingleItem().Name.ShouldBe("Delivery");
        byDescription.Items.ShouldHaveSingleItem().Name.ShouldBe("Billing");
    }

    [Fact]
    public async Task ListCapsPageSize()
    {
        var page = await _fixture.List.HandleAsync(new ListAddressTypes.Query(PageSize: 2), Ct);
        var huge = await _fixture.List.HandleAsync(new ListAddressTypes.Query(PageSize: 10_000), Ct);

        page.Items.Count.ShouldBe(2);
        page.TotalCount.ShouldBe(4);
        huge.Items.Count.ShouldBe(4);
    }

    [Fact]
    public async Task UpdateChangesFieldsAndClearsTheDescription()
    {
        var created = (await _fixture.Create.HandleAsync(new CreateAddressType.Command("Postal", "PO box"), Ct)).Value;

        var result = await _fixture.Update.HandleAsync(new UpdateAddressType.Command(created.Id, "Mailing", ""), Ct);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Name.ShouldBe("Mailing");
        result.Value.Description.ShouldBeNull();
    }

    [Fact]
    public async Task UpdateRejectsNameOfAnotherTypeButAllowsChangingCaseOfItsOwn()
    {
        var created = (await _fixture.Create.HandleAsync(new CreateAddressType.Command("Postal", null), Ct)).Value;

        var clash = await _fixture.Update.HandleAsync(new UpdateAddressType.Command(created.Id, "billing", null), Ct);
        var own = await _fixture.Update.HandleAsync(new UpdateAddressType.Command(created.Id, "POSTAL", null), Ct);

        clash.Error.Code.ShouldBe("address_type_conflict");
        own.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public async Task UpdateReturnsNotFoundForUnknownId()
    {
        var result = await _fixture.Update.HandleAsync(new UpdateAddressType.Command(Guid.NewGuid(), "Postal", null), Ct);

        result.Error.Code.ShouldBe("address_type_not_found");
    }

    [Fact]
    public async Task DeleteRemovesTheTypeAndReportsNotFoundTheSecondTime()
    {
        var created = (await _fixture.Create.HandleAsync(new CreateAddressType.Command("Postal", null), Ct)).Value;

        var first = await _fixture.Delete.HandleAsync(new DeleteAddressType.Command(created.Id), Ct);
        var second = await _fixture.Delete.HandleAsync(new DeleteAddressType.Command(created.Id), Ct);

        first.IsSuccess.ShouldBeTrue();
        second.Error.Code.ShouldBe("address_type_not_found");
    }
}
