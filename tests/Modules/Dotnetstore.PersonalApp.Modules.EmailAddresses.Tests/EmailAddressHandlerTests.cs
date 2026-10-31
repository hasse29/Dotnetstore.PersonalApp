using Dotnetstore.PersonalApp.Modules.EmailAddresses.Features.EmailAddresses;
using Dotnetstore.PersonalApp.Modules.EmailAddresses.Features.EmailAddressTypes;

namespace Dotnetstore.PersonalApp.Modules.EmailAddresses.Tests;

public sealed class EmailAddressHandlerTests : IDisposable
{
    private readonly EmailAddressesFixture _fixture = new();
    private readonly Guid _owner = Guid.NewGuid();

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public void Dispose() => _fixture.Dispose();

    [Fact]
    public async Task CreateStoresTheTrimmedEmailAddressForTheObject()
    {
        var result = await _fixture.CreateEmailAddress.HandleAsync(_fixture.ValidEmailAddress(_owner, email: "  ada@example.com "), Ct);

        Assert.True(result.IsSuccess);
        Assert.Equal(_owner, result.Value.ObjectId);
        Assert.Equal("Billing", result.Value.EmailAddressTypeName);
        Assert.Equal("ada@example.com", result.Value.Email);
    }

    [Theory]
    [InlineData("", "email_address_email_required")]
    [InlineData("   ", "email_address_email_required")]
    [InlineData("not-an-email", "email_address_email_invalid")]
    [InlineData("ada@", "email_address_email_invalid")]
    public async Task CreateRejectsMissingOrInvalidEmail(string email, string expectedCode)
    {
        var result = await _fixture.CreateEmailAddress.HandleAsync(_fixture.ValidEmailAddress(_owner, email: email), Ct);

        Assert.Equal(expectedCode, result.Error.Code);
    }

    [Fact]
    public async Task CreateRejectsTooLongEmail()
    {
        var result = await _fixture.CreateEmailAddress.HandleAsync(
            _fixture.ValidEmailAddress(_owner, email: new string('a', 250) + "@x.se"), Ct);

        Assert.Equal("email_address_email_too_long", result.Error.Code);
    }

    [Fact]
    public async Task CreateRequiresAnObjectAndAKnownType()
    {
        var noObject = await _fixture.CreateEmailAddress.HandleAsync(_fixture.ValidEmailAddress(Guid.Empty), Ct);
        var noType = await _fixture.CreateEmailAddress.HandleAsync(_fixture.ValidEmailAddress(_owner) with { EmailAddressTypeId = Guid.Empty }, Ct);
        var unknownType = await _fixture.CreateEmailAddress.HandleAsync(_fixture.ValidEmailAddress(_owner, Guid.NewGuid()), Ct);

        Assert.Equal("email_address_object_required", noObject.Error.Code);
        Assert.Equal("email_address_type_required", noType.Error.Code);
        Assert.Equal("email_address_type_not_found", unknownType.Error.Code);
    }

    [Fact]
    public async Task AnObjectCannotRegisterTheSameEmailTwiceButOthersCan()
    {
        await _fixture.CreateEmailAddress.HandleAsync(_fixture.ValidEmailAddress(_owner), Ct);

        var duplicate = await _fixture.CreateEmailAddress.HandleAsync(
            _fixture.ValidEmailAddress(_owner, _fixture.WorkTypeId, "ADA@example.com"), Ct);
        var otherObject = await _fixture.CreateEmailAddress.HandleAsync(_fixture.ValidEmailAddress(Guid.NewGuid()), Ct);

        Assert.Equal("email_address_conflict", duplicate.Error.Code);
        Assert.True(otherObject.IsSuccess);
    }

    [Fact]
    public async Task ListIsPerObjectOrderedByTypeThenEmail()
    {
        var other = Guid.NewGuid();
        await _fixture.CreateEmailAddress.HandleAsync(_fixture.ValidEmailAddress(_owner, email: "b@example.com"), Ct);
        await _fixture.CreateEmailAddress.HandleAsync(_fixture.ValidEmailAddress(_owner, email: "a@example.com"), Ct);
        await _fixture.CreateEmailAddress.HandleAsync(_fixture.ValidEmailAddress(_owner, _fixture.WorkTypeId, "c@example.com"), Ct);
        await _fixture.CreateEmailAddress.HandleAsync(_fixture.ValidEmailAddress(other), Ct);

        var mine = await _fixture.ListEmailAddresses.HandleAsync(new ListEmailAddresses.Query(_owner), Ct);
        var theirs = await _fixture.ListEmailAddresses.HandleAsync(new ListEmailAddresses.Query(other), Ct);

        Assert.Equal(3, mine.TotalCount);
        Assert.Equal(["a@example.com", "b@example.com", "c@example.com"], mine.Items.Select(e => e.Email));
        Assert.Single(theirs.Items);
    }

    [Fact]
    public async Task ListCapsPageSize()
    {
        for (var i = 0; i < 3; i++)
        {
            await _fixture.CreateEmailAddress.HandleAsync(_fixture.ValidEmailAddress(_owner, email: $"user{i}@example.com"), Ct);
        }

        var page = await _fixture.ListEmailAddresses.HandleAsync(new ListEmailAddresses.Query(_owner, PageSize: 2), Ct);
        var huge = await _fixture.ListEmailAddresses.HandleAsync(new ListEmailAddresses.Query(_owner, PageSize: 10_000), Ct);

        Assert.Equal(2, page.Items.Count);
        Assert.Equal(3, page.TotalCount);
        Assert.Equal(3, huge.Items.Count);
    }

    [Fact]
    public async Task GetReturnsTheEmailAddressOrNotFound()
    {
        var created = (await _fixture.CreateEmailAddress.HandleAsync(_fixture.ValidEmailAddress(_owner), Ct)).Value;

        var found = await _fixture.GetEmailAddress.HandleAsync(new GetEmailAddress.Query(created.Id), Ct);
        var missing = await _fixture.GetEmailAddress.HandleAsync(new GetEmailAddress.Query(Guid.NewGuid()), Ct);

        Assert.Equal("Billing", found.Value.EmailAddressTypeName);
        Assert.Equal("email_address_not_found", missing.Error.Code);
    }

    [Fact]
    public async Task UpdateChangesTypeAndEmailButNotTheOwner()
    {
        var created = (await _fixture.CreateEmailAddress.HandleAsync(_fixture.ValidEmailAddress(_owner), Ct)).Value;

        var result = await _fixture.UpdateEmailAddress.HandleAsync(
            new UpdateEmailAddress.Command(created.Id, _fixture.WorkTypeId, "ada@work.example"), Ct);

        Assert.True(result.IsSuccess);
        Assert.Equal(_owner, result.Value.ObjectId);
        Assert.Equal("Work", result.Value.EmailAddressTypeName);
        Assert.Equal("ada@work.example", result.Value.Email);
    }

    [Fact]
    public async Task UpdateRejectsAnotherOfTheSameObjectsAddressesButAllowsKeepingItsOwn()
    {
        await _fixture.CreateEmailAddress.HandleAsync(_fixture.ValidEmailAddress(_owner, email: "one@example.com"), Ct);
        var two = (await _fixture.CreateEmailAddress.HandleAsync(_fixture.ValidEmailAddress(_owner, email: "two@example.com"), Ct)).Value;

        var clash = await _fixture.UpdateEmailAddress.HandleAsync(new UpdateEmailAddress.Command(two.Id, two.EmailAddressTypeId, "ONE@example.com"), Ct);
        var own = await _fixture.UpdateEmailAddress.HandleAsync(new UpdateEmailAddress.Command(two.Id, two.EmailAddressTypeId, "TWO@example.com"), Ct);

        Assert.Equal("email_address_conflict", clash.Error.Code);
        Assert.True(own.IsSuccess);
    }

    [Fact]
    public async Task UpdateReportsUnknownEmailAddressAndUnknownType()
    {
        var created = (await _fixture.CreateEmailAddress.HandleAsync(_fixture.ValidEmailAddress(_owner), Ct)).Value;

        var unknownAddress = await _fixture.UpdateEmailAddress.HandleAsync(
            new UpdateEmailAddress.Command(Guid.NewGuid(), _fixture.BillingTypeId, "x@example.com"), Ct);
        var unknownType = await _fixture.UpdateEmailAddress.HandleAsync(
            new UpdateEmailAddress.Command(created.Id, Guid.NewGuid(), "x@example.com"), Ct);

        Assert.Equal("email_address_not_found", unknownAddress.Error.Code);
        Assert.Equal("email_address_type_not_found", unknownType.Error.Code);
    }

    [Fact]
    public async Task DeleteRemovesTheEmailAddressAndReportsNotFoundTheSecondTime()
    {
        var created = (await _fixture.CreateEmailAddress.HandleAsync(_fixture.ValidEmailAddress(_owner), Ct)).Value;

        var first = await _fixture.DeleteEmailAddress.HandleAsync(new DeleteEmailAddress.Command(created.Id), Ct);
        var second = await _fixture.DeleteEmailAddress.HandleAsync(new DeleteEmailAddress.Command(created.Id), Ct);

        Assert.True(first.IsSuccess);
        Assert.Equal("email_address_not_found", second.Error.Code);
    }

    [Fact]
    public async Task DeleteAllRemovesOnlyTheObjectsEmailAddressesAndReportsTheCount()
    {
        var other = Guid.NewGuid();
        await _fixture.CreateEmailAddress.HandleAsync(_fixture.ValidEmailAddress(_owner, email: "a@example.com"), Ct);
        await _fixture.CreateEmailAddress.HandleAsync(_fixture.ValidEmailAddress(_owner, _fixture.WorkTypeId, "b@example.com"), Ct);
        await _fixture.CreateEmailAddress.HandleAsync(_fixture.ValidEmailAddress(other), Ct);

        var result = await _fixture.DeleteEmailAddressesForObject.HandleAsync(new DeleteEmailAddressesForObject.Command(_owner), Ct);
        var again = await _fixture.DeleteEmailAddressesForObject.HandleAsync(new DeleteEmailAddressesForObject.Command(_owner), Ct);

        Assert.Equal(2, result.Value);
        Assert.Equal(0, again.Value);
        Assert.Equal(1, (await _fixture.ListEmailAddresses.HandleAsync(new ListEmailAddresses.Query(other), Ct)).TotalCount);
    }

    [Fact]
    public async Task DeleteAllRejectsAnEmptyObjectId()
    {
        var result = await _fixture.DeleteEmailAddressesForObject.HandleAsync(new DeleteEmailAddressesForObject.Command(Guid.Empty), Ct);

        Assert.Equal("email_address_object_required", result.Error.Code);
    }

    [Fact]
    public async Task EmailAddressTypeInUseCannotBeDeletedUntilItsEmailAddressesAreGone()
    {
        var address = (await _fixture.CreateEmailAddress.HandleAsync(_fixture.ValidEmailAddress(_owner), Ct)).Value;

        var blocked = await _fixture.Delete.HandleAsync(new DeleteEmailAddressType.Command(address.EmailAddressTypeId), Ct);
        await _fixture.DeleteEmailAddress.HandleAsync(new DeleteEmailAddress.Command(address.Id), Ct);
        var allowed = await _fixture.Delete.HandleAsync(new DeleteEmailAddressType.Command(address.EmailAddressTypeId), Ct);

        Assert.Equal("email_address_type_in_use", blocked.Error.Code);
        Assert.True(allowed.IsSuccess);
    }
}