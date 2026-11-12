using Dotnetstore.PersonalApp.Modules.Contact.PhoneNumbers;
using Dotnetstore.PersonalApp.Modules.Contact.PhoneNumbers.Features.PhoneNumbers;
using Dotnetstore.PersonalApp.Modules.Contact.PhoneNumbers.Features.PhoneNumberTypes;

namespace Dotnetstore.PersonalApp.Modules.Contact.Tests.PhoneNumbers;

public sealed class PhoneNumberHandlerTests : IDisposable
{
    private readonly PhoneNumbersFixture _fixture = new();
    private readonly Guid _owner = Guid.NewGuid();

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public void Dispose() => _fixture.Dispose();

    [Fact]
    public async Task CreateStoresTheTrimmedNumberAsTypedForTheObject()
    {
        var result = await _fixture.CreatePhoneNumber.HandleAsync(_fixture.ValidPhoneNumber(_owner, number: "  +46 (0)8 123 45 "), Ct);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ObjectId.ShouldBe(_owner);
        result.Value.PhoneNumberTypeName.ShouldBe("Mobile");
        result.Value.Number.ShouldBe("+46 (0)8 123 45");
    }

    [Theory]
    [InlineData("070-123 45 67")]
    [InlineData("+46 70 123 45 67")]
    [InlineData("(08) 123.456")]
    [InlineData("08/123456")]
    public async Task CreateAcceptsCommonFormats(string number)
    {
        var result = await _fixture.CreatePhoneNumber.HandleAsync(_fixture.ValidPhoneNumber(_owner, number: number), Ct);

        result.IsSuccess.ShouldBeTrue();
    }

    [Theory]
    [InlineData("", "phone_number_number_required")]
    [InlineData("   ", "phone_number_number_required")]
    [InlineData("abc", "phone_number_number_invalid")]
    [InlineData("0701234x67", "phone_number_number_invalid")]
    [InlineData("1234", "phone_number_number_invalid")]
    [InlineData("1234567890123456", "phone_number_number_invalid")]
    [InlineData("070+1234567", "phone_number_number_invalid")]
    public async Task CreateRejectsMissingOrInvalidNumbers(string number, string expectedCode)
    {
        var result = await _fixture.CreatePhoneNumber.HandleAsync(_fixture.ValidPhoneNumber(_owner, number: number), Ct);

        result.Error.Code.ShouldBe(expectedCode);
    }

    [Fact]
    public async Task CreateRejectsTooLongNumber()
    {
        var result = await _fixture.CreatePhoneNumber.HandleAsync(_fixture.ValidPhoneNumber(_owner, number: new string('1', 31)), Ct);

        result.Error.Code.ShouldBe("phone_number_number_too_long");
    }

    [Fact]
    public async Task CreateRequiresAnObjectAndAKnownType()
    {
        var noObject = await _fixture.CreatePhoneNumber.HandleAsync(_fixture.ValidPhoneNumber(Guid.Empty), Ct);
        var noType = await _fixture.CreatePhoneNumber.HandleAsync(_fixture.ValidPhoneNumber(_owner) with { PhoneNumberTypeId = Guid.Empty }, Ct);
        var unknownType = await _fixture.CreatePhoneNumber.HandleAsync(_fixture.ValidPhoneNumber(_owner, Guid.NewGuid()), Ct);

        noObject.Error.Code.ShouldBe("phone_number_object_required");
        noType.Error.Code.ShouldBe("phone_number_type_required");
        unknownType.Error.Code.ShouldBe("phone_number_type_not_found");
    }

    [Fact]
    public async Task AnObjectCannotRegisterTheSameNumberTwiceHoweverItIsFormattedButOthersCan()
    {
        await _fixture.CreatePhoneNumber.HandleAsync(_fixture.ValidPhoneNumber(_owner, number: "+46 8 123 456"), Ct);

        var duplicate = await _fixture.CreatePhoneNumber.HandleAsync(
            _fixture.ValidPhoneNumber(_owner, _fixture.WorkTypeId, "+46-8-123456"), Ct);
        var withoutPlus = await _fixture.CreatePhoneNumber.HandleAsync(_fixture.ValidPhoneNumber(_owner, number: "46 8 123 456"), Ct);
        var otherObject = await _fixture.CreatePhoneNumber.HandleAsync(
            _fixture.ValidPhoneNumber(Guid.NewGuid(), number: "+46 8 123 456"), Ct);

        duplicate.Error.Code.ShouldBe("phone_number_conflict");
        withoutPlus.IsSuccess.ShouldBeTrue();
        otherObject.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public async Task ListIsPerObjectOrderedByTypeThenNumber()
    {
        var other = Guid.NewGuid();
        await _fixture.CreatePhoneNumber.HandleAsync(_fixture.ValidPhoneNumber(_owner, number: "070 222 22 22"), Ct);
        await _fixture.CreatePhoneNumber.HandleAsync(_fixture.ValidPhoneNumber(_owner, number: "070 111 11 11"), Ct);
        await _fixture.CreatePhoneNumber.HandleAsync(_fixture.ValidPhoneNumber(_owner, _fixture.WorkTypeId, "08 123 456"), Ct);
        await _fixture.CreatePhoneNumber.HandleAsync(_fixture.ValidPhoneNumber(other), Ct);

        var mine = await _fixture.ListPhoneNumbers.HandleAsync(new ListPhoneNumbers.Query(_owner), Ct);
        var theirs = await _fixture.ListPhoneNumbers.HandleAsync(new ListPhoneNumbers.Query(other), Ct);

        mine.TotalCount.ShouldBe(3);
        mine.Items.Select(p => p.Number).ShouldBe(["070 111 11 11", "070 222 22 22", "08 123 456"]);
        theirs.Items.ShouldHaveSingleItem();
    }

    [Fact]
    public async Task ListCapsPageSize()
    {
        for (var i = 0; i < 3; i++)
        {
            await _fixture.CreatePhoneNumber.HandleAsync(_fixture.ValidPhoneNumber(_owner, number: $"070 000 00 0{i}"), Ct);
        }

        var page = await _fixture.ListPhoneNumbers.HandleAsync(new ListPhoneNumbers.Query(_owner, PageSize: 2), Ct);
        var huge = await _fixture.ListPhoneNumbers.HandleAsync(new ListPhoneNumbers.Query(_owner, PageSize: 10_000), Ct);

        page.Items.Count.ShouldBe(2);
        page.TotalCount.ShouldBe(3);
        huge.Items.Count.ShouldBe(3);
    }

    [Fact]
    public async Task GetReturnsThePhoneNumberOrNotFound()
    {
        var created = (await _fixture.CreatePhoneNumber.HandleAsync(_fixture.ValidPhoneNumber(_owner), Ct)).Value;

        var found = await _fixture.GetPhoneNumber.HandleAsync(new GetPhoneNumber.Query(created.Id), Ct);
        var missing = await _fixture.GetPhoneNumber.HandleAsync(new GetPhoneNumber.Query(Guid.NewGuid()), Ct);

        found.Value.PhoneNumberTypeName.ShouldBe("Mobile");
        missing.Error.Code.ShouldBe("phone_number_not_found");
    }

    [Fact]
    public async Task UpdateChangesTypeAndNumberButNotTheOwner()
    {
        var created = (await _fixture.CreatePhoneNumber.HandleAsync(_fixture.ValidPhoneNumber(_owner), Ct)).Value;

        var result = await _fixture.UpdatePhoneNumber.HandleAsync(
            new UpdatePhoneNumber.Command(created.Id, _fixture.WorkTypeId, "08 555 12 34"), Ct);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ObjectId.ShouldBe(_owner);
        result.Value.PhoneNumberTypeName.ShouldBe("Work");
        result.Value.Number.ShouldBe("08 555 12 34");
    }

    [Fact]
    public async Task UpdateRejectsAnotherOfTheSameObjectsNumbersButAllowsReformattingItsOwn()
    {
        await _fixture.CreatePhoneNumber.HandleAsync(_fixture.ValidPhoneNumber(_owner, number: "070 111 11 11"), Ct);
        var two = (await _fixture.CreatePhoneNumber.HandleAsync(_fixture.ValidPhoneNumber(_owner, number: "070 222 22 22"), Ct)).Value;

        var clash = await _fixture.UpdatePhoneNumber.HandleAsync(new UpdatePhoneNumber.Command(two.Id, two.PhoneNumberTypeId, "070-111-1111"), Ct);
        var own = await _fixture.UpdatePhoneNumber.HandleAsync(new UpdatePhoneNumber.Command(two.Id, two.PhoneNumberTypeId, "070-222-2222"), Ct);

        clash.Error.Code.ShouldBe("phone_number_conflict");
        own.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public async Task UpdateReportsUnknownPhoneNumberAndUnknownType()
    {
        var created = (await _fixture.CreatePhoneNumber.HandleAsync(_fixture.ValidPhoneNumber(_owner), Ct)).Value;

        var unknownNumber = await _fixture.UpdatePhoneNumber.HandleAsync(
            new UpdatePhoneNumber.Command(Guid.NewGuid(), _fixture.MobileTypeId, "070 123 45 67"), Ct);
        var unknownType = await _fixture.UpdatePhoneNumber.HandleAsync(
            new UpdatePhoneNumber.Command(created.Id, Guid.NewGuid(), "070 123 45 67"), Ct);

        unknownNumber.Error.Code.ShouldBe("phone_number_not_found");
        unknownType.Error.Code.ShouldBe("phone_number_type_not_found");
    }

    [Fact]
    public async Task DeleteRemovesThePhoneNumberAndReportsNotFoundTheSecondTime()
    {
        var created = (await _fixture.CreatePhoneNumber.HandleAsync(_fixture.ValidPhoneNumber(_owner), Ct)).Value;

        var first = await _fixture.DeletePhoneNumber.HandleAsync(new DeletePhoneNumber.Command(created.Id), Ct);
        var second = await _fixture.DeletePhoneNumber.HandleAsync(new DeletePhoneNumber.Command(created.Id), Ct);

        first.IsSuccess.ShouldBeTrue();
        second.Error.Code.ShouldBe("phone_number_not_found");
    }

    [Fact]
    public async Task DeleteAllRemovesOnlyTheObjectsPhoneNumbersAndReportsTheCount()
    {
        var other = Guid.NewGuid();
        await _fixture.CreatePhoneNumber.HandleAsync(_fixture.ValidPhoneNumber(_owner, number: "070 111 11 11"), Ct);
        await _fixture.CreatePhoneNumber.HandleAsync(_fixture.ValidPhoneNumber(_owner, _fixture.WorkTypeId, "08 123 456"), Ct);
        await _fixture.CreatePhoneNumber.HandleAsync(_fixture.ValidPhoneNumber(other), Ct);

        var result = await _fixture.DeletePhoneNumbersForObject.HandleAsync(new DeletePhoneNumbersForObject.Command(_owner), Ct);
        var again = await _fixture.DeletePhoneNumbersForObject.HandleAsync(new DeletePhoneNumbersForObject.Command(_owner), Ct);

        result.Value.ShouldBe(2);
        again.Value.ShouldBe(0);
        (await _fixture.ListPhoneNumbers.HandleAsync(new ListPhoneNumbers.Query(other), Ct)).TotalCount.ShouldBe(1);
    }

    [Fact]
    public async Task DeleteAllRejectsAnEmptyObjectId()
    {
        var result = await _fixture.DeletePhoneNumbersForObject.HandleAsync(new DeletePhoneNumbersForObject.Command(Guid.Empty), Ct);

        result.Error.Code.ShouldBe("phone_number_object_required");
    }

    [Fact]
    public async Task PhoneNumberTypeInUseCannotBeDeletedUntilItsPhoneNumbersAreGone()
    {
        var phone = (await _fixture.CreatePhoneNumber.HandleAsync(_fixture.ValidPhoneNumber(_owner), Ct)).Value;

        var blocked = await _fixture.Delete.HandleAsync(new DeletePhoneNumberType.Command(phone.PhoneNumberTypeId), Ct);
        await _fixture.DeletePhoneNumber.HandleAsync(new DeletePhoneNumber.Command(phone.Id), Ct);
        var allowed = await _fixture.Delete.HandleAsync(new DeletePhoneNumberType.Command(phone.PhoneNumberTypeId), Ct);

        blocked.Error.Code.ShouldBe("phone_number_type_in_use");
        allowed.IsSuccess.ShouldBeTrue();
    }
}
