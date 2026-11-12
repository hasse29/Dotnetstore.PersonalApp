using Dotnetstore.PersonalApp.Modules.Contact.PhoneNumbers;
using Dotnetstore.PersonalApp.Modules.Contact.PhoneNumbers.Features.PhoneNumberTypes;

namespace Dotnetstore.PersonalApp.Modules.Contact.Tests.PhoneNumbers;

public sealed class PhoneNumberTypeHandlerTests : IDisposable
{
    private readonly PhoneNumbersFixture _fixture = new();

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public void Dispose() => _fixture.Dispose();

    [Fact]
    public async Task DefaultTypesAreSeeded()
    {
        var all = await _fixture.List.HandleAsync(new ListPhoneNumberTypes.Query(), Ct);

        all.Items.Select(t => t.Name).ShouldBe(["Home", "Mobile", "Work"]);
    }

    [Fact]
    public async Task CreateStoresTheTypeWithNormalizedFields()
    {
        var result = await _fixture.Create.HandleAsync(new CreatePhoneNumberType.Command("  Postal  ", "   "), Ct);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Name.ShouldBe("Postal");
        result.Value.Description.ShouldBeNull();
        result.Value.CreatedAt.ShouldBe(new DateTimeOffset(2026, 10, 31, 12, 0, 0, TimeSpan.Zero));
    }

    [Theory]
    [InlineData("", "phone_number_type_name_required")]
    [InlineData("   ", "phone_number_type_name_required")]
    public async Task CreateRejectsInvalidInput(string name, string expectedCode)
    {
        var result = await _fixture.Create.HandleAsync(new CreatePhoneNumberType.Command(name, null), Ct);

        result.IsSuccess.ShouldBeFalse();
        result.Error.Code.ShouldBe(expectedCode);
    }

    [Fact]
    public async Task CreateRejectsTooLongValues()
    {
        var name = await _fixture.Create.HandleAsync(new CreatePhoneNumberType.Command(new string('x', 101), null), Ct);
        var description = await _fixture.Create.HandleAsync(new CreatePhoneNumberType.Command("Long", new string('x', 501)), Ct);

        name.Error.Code.ShouldBe("phone_number_type_name_too_long");
        description.Error.Code.ShouldBe("phone_number_type_description_too_long");
    }

    [Fact]
    public async Task CreateRejectsDuplicateNameIgnoringCaseAndSurroundingSpaces()
    {
        var result = await _fixture.Create.HandleAsync(new CreatePhoneNumberType.Command("  mOBILE ", null), Ct);

        result.Error.Code.ShouldBe("phone_number_type_conflict");
    }

    [Fact]
    public async Task GetReturnsTheTypeOrNotFound()
    {
        var created = (await _fixture.Create.HandleAsync(new CreatePhoneNumberType.Command("Postal", "PO box"), Ct)).Value;

        var found = await _fixture.Get.HandleAsync(new GetPhoneNumberType.Query(created.Id), Ct);
        var missing = await _fixture.Get.HandleAsync(new GetPhoneNumberType.Query(Guid.NewGuid()), Ct);

        found.Value.Description.ShouldBe("PO box");
        missing.Error.Code.ShouldBe("phone_number_type_not_found");
    }

    [Fact]
    public async Task ListSearchesNameAndDescription()
    {
        var byName = await _fixture.List.HandleAsync(new ListPhoneNumberTypes.Query("mob"), Ct);
        var byDescription = await _fixture.List.HandleAsync(new ListPhoneNumberTypes.Query("landline"), Ct);

        byName.Items.ShouldHaveSingleItem().Name.ShouldBe("Mobile");
        byDescription.Items.ShouldHaveSingleItem().Name.ShouldBe("Home");
    }

    [Fact]
    public async Task ListCapsPageSize()
    {
        var page = await _fixture.List.HandleAsync(new ListPhoneNumberTypes.Query(PageSize: 2), Ct);
        var huge = await _fixture.List.HandleAsync(new ListPhoneNumberTypes.Query(PageSize: 10_000), Ct);

        page.Items.Count.ShouldBe(2);
        page.TotalCount.ShouldBe(3);
        huge.Items.Count.ShouldBe(3);
    }

    [Fact]
    public async Task UpdateChangesFieldsAndClearsTheDescription()
    {
        var created = (await _fixture.Create.HandleAsync(new CreatePhoneNumberType.Command("Postal", "PO box"), Ct)).Value;

        var result = await _fixture.Update.HandleAsync(new UpdatePhoneNumberType.Command(created.Id, "Mailing", ""), Ct);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Name.ShouldBe("Mailing");
        result.Value.Description.ShouldBeNull();
    }

    [Fact]
    public async Task UpdateRejectsNameOfAnotherTypeButAllowsChangingCaseOfItsOwn()
    {
        var created = (await _fixture.Create.HandleAsync(new CreatePhoneNumberType.Command("Postal", null), Ct)).Value;

        var clash = await _fixture.Update.HandleAsync(new UpdatePhoneNumberType.Command(created.Id, "mobile", null), Ct);
        var own = await _fixture.Update.HandleAsync(new UpdatePhoneNumberType.Command(created.Id, "POSTAL", null), Ct);

        clash.Error.Code.ShouldBe("phone_number_type_conflict");
        own.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public async Task UpdateReturnsNotFoundForUnknownId()
    {
        var result = await _fixture.Update.HandleAsync(new UpdatePhoneNumberType.Command(Guid.NewGuid(), "Postal", null), Ct);

        result.Error.Code.ShouldBe("phone_number_type_not_found");
    }

    [Fact]
    public async Task DeleteRemovesTheTypeAndReportsNotFoundTheSecondTime()
    {
        var created = (await _fixture.Create.HandleAsync(new CreatePhoneNumberType.Command("Postal", null), Ct)).Value;

        var first = await _fixture.Delete.HandleAsync(new DeletePhoneNumberType.Command(created.Id), Ct);
        var second = await _fixture.Delete.HandleAsync(new DeletePhoneNumberType.Command(created.Id), Ct);

        first.IsSuccess.ShouldBeTrue();
        second.Error.Code.ShouldBe("phone_number_type_not_found");
    }
}
