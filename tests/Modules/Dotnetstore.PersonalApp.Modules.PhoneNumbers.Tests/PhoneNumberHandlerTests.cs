using Dotnetstore.PersonalApp.Modules.PhoneNumbers.Features.PhoneNumbers;
using Dotnetstore.PersonalApp.Modules.PhoneNumbers.Features.PhoneNumberTypes;

namespace Dotnetstore.PersonalApp.Modules.PhoneNumbers.Tests;

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

        Assert.True(result.IsSuccess);
        Assert.Equal(_owner, result.Value.ObjectId);
        Assert.Equal("Mobile", result.Value.PhoneNumberTypeName);
        Assert.Equal("+46 (0)8 123 45", result.Value.Number);
    }

    [Theory]
    [InlineData("070-123 45 67")]
    [InlineData("+46 70 123 45 67")]
    [InlineData("(08) 123.456")]
    [InlineData("08/123456")]
    public async Task CreateAcceptsCommonFormats(string number)
    {
        var result = await _fixture.CreatePhoneNumber.HandleAsync(_fixture.ValidPhoneNumber(_owner, number: number), Ct);

        Assert.True(result.IsSuccess);
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

        Assert.Equal(expectedCode, result.Error.Code);
    }

    [Fact]
    public async Task CreateRejectsTooLongNumber()
    {
        var result = await _fixture.CreatePhoneNumber.HandleAsync(_fixture.ValidPhoneNumber(_owner, number: new string('1', 31)), Ct);

        Assert.Equal("phone_number_number_too_long", result.Error.Code);
    }

    [Fact]
    public async Task CreateRequiresAnObjectAndAKnownType()
    {
        var noObject = await _fixture.CreatePhoneNumber.HandleAsync(_fixture.ValidPhoneNumber(Guid.Empty), Ct);
        var noType = await _fixture.CreatePhoneNumber.HandleAsync(_fixture.ValidPhoneNumber(_owner) with { PhoneNumberTypeId = Guid.Empty }, Ct);
        var unknownType = await _fixture.CreatePhoneNumber.HandleAsync(_fixture.ValidPhoneNumber(_owner, Guid.NewGuid()), Ct);

        Assert.Equal("phone_number_object_required", noObject.Error.Code);
        Assert.Equal("phone_number_type_required", noType.Error.Code);
        Assert.Equal("phone_number_type_not_found", unknownType.Error.Code);
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

        Assert.Equal("phone_number_conflict", duplicate.Error.Code);
        Assert.True(withoutPlus.IsSuccess);
        Assert.True(otherObject.IsSuccess);
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

        Assert.Equal(3, mine.TotalCount);
        Assert.Equal(["070 111 11 11", "070 222 22 22", "08 123 456"], mine.Items.Select(p => p.Number));
        Assert.Single(theirs.Items);
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

        Assert.Equal(2, page.Items.Count);
        Assert.Equal(3, page.TotalCount);
        Assert.Equal(3, huge.Items.Count);
    }

    [Fact]
    public async Task GetReturnsThePhoneNumberOrNotFound()
    {
        var created = (await _fixture.CreatePhoneNumber.HandleAsync(_fixture.ValidPhoneNumber(_owner), Ct)).Value;

        var found = await _fixture.GetPhoneNumber.HandleAsync(new GetPhoneNumber.Query(created.Id), Ct);
        var missing = await _fixture.GetPhoneNumber.HandleAsync(new GetPhoneNumber.Query(Guid.NewGuid()), Ct);

        Assert.Equal("Mobile", found.Value.PhoneNumberTypeName);
        Assert.Equal("phone_number_not_found", missing.Error.Code);
    }

    [Fact]
    public async Task UpdateChangesTypeAndNumberButNotTheOwner()
    {
        var created = (await _fixture.CreatePhoneNumber.HandleAsync(_fixture.ValidPhoneNumber(_owner), Ct)).Value;

        var result = await _fixture.UpdatePhoneNumber.HandleAsync(
            new UpdatePhoneNumber.Command(created.Id, _fixture.WorkTypeId, "08 555 12 34"), Ct);

        Assert.True(result.IsSuccess);
        Assert.Equal(_owner, result.Value.ObjectId);
        Assert.Equal("Work", result.Value.PhoneNumberTypeName);
        Assert.Equal("08 555 12 34", result.Value.Number);
    }

    [Fact]
    public async Task UpdateRejectsAnotherOfTheSameObjectsNumbersButAllowsReformattingItsOwn()
    {
        await _fixture.CreatePhoneNumber.HandleAsync(_fixture.ValidPhoneNumber(_owner, number: "070 111 11 11"), Ct);
        var two = (await _fixture.CreatePhoneNumber.HandleAsync(_fixture.ValidPhoneNumber(_owner, number: "070 222 22 22"), Ct)).Value;

        var clash = await _fixture.UpdatePhoneNumber.HandleAsync(new UpdatePhoneNumber.Command(two.Id, two.PhoneNumberTypeId, "070-111-1111"), Ct);
        var own = await _fixture.UpdatePhoneNumber.HandleAsync(new UpdatePhoneNumber.Command(two.Id, two.PhoneNumberTypeId, "070-222-2222"), Ct);

        Assert.Equal("phone_number_conflict", clash.Error.Code);
        Assert.True(own.IsSuccess);
    }

    [Fact]
    public async Task UpdateReportsUnknownPhoneNumberAndUnknownType()
    {
        var created = (await _fixture.CreatePhoneNumber.HandleAsync(_fixture.ValidPhoneNumber(_owner), Ct)).Value;

        var unknownNumber = await _fixture.UpdatePhoneNumber.HandleAsync(
            new UpdatePhoneNumber.Command(Guid.NewGuid(), _fixture.MobileTypeId, "070 123 45 67"), Ct);
        var unknownType = await _fixture.UpdatePhoneNumber.HandleAsync(
            new UpdatePhoneNumber.Command(created.Id, Guid.NewGuid(), "070 123 45 67"), Ct);

        Assert.Equal("phone_number_not_found", unknownNumber.Error.Code);
        Assert.Equal("phone_number_type_not_found", unknownType.Error.Code);
    }

    [Fact]
    public async Task DeleteRemovesThePhoneNumberAndReportsNotFoundTheSecondTime()
    {
        var created = (await _fixture.CreatePhoneNumber.HandleAsync(_fixture.ValidPhoneNumber(_owner), Ct)).Value;

        var first = await _fixture.DeletePhoneNumber.HandleAsync(new DeletePhoneNumber.Command(created.Id), Ct);
        var second = await _fixture.DeletePhoneNumber.HandleAsync(new DeletePhoneNumber.Command(created.Id), Ct);

        Assert.True(first.IsSuccess);
        Assert.Equal("phone_number_not_found", second.Error.Code);
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

        Assert.Equal(2, result.Value);
        Assert.Equal(0, again.Value);
        Assert.Equal(1, (await _fixture.ListPhoneNumbers.HandleAsync(new ListPhoneNumbers.Query(other), Ct)).TotalCount);
    }

    [Fact]
    public async Task DeleteAllRejectsAnEmptyObjectId()
    {
        var result = await _fixture.DeletePhoneNumbersForObject.HandleAsync(new DeletePhoneNumbersForObject.Command(Guid.Empty), Ct);

        Assert.Equal("phone_number_object_required", result.Error.Code);
    }

    [Fact]
    public async Task PhoneNumberTypeInUseCannotBeDeletedUntilItsPhoneNumbersAreGone()
    {
        var phone = (await _fixture.CreatePhoneNumber.HandleAsync(_fixture.ValidPhoneNumber(_owner), Ct)).Value;

        var blocked = await _fixture.Delete.HandleAsync(new DeletePhoneNumberType.Command(phone.PhoneNumberTypeId), Ct);
        await _fixture.DeletePhoneNumber.HandleAsync(new DeletePhoneNumber.Command(phone.Id), Ct);
        var allowed = await _fixture.Delete.HandleAsync(new DeletePhoneNumberType.Command(phone.PhoneNumberTypeId), Ct);

        Assert.Equal("phone_number_type_in_use", blocked.Error.Code);
        Assert.True(allowed.IsSuccess);
    }
}