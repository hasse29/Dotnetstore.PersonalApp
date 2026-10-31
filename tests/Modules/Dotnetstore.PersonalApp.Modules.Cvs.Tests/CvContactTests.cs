using Dotnetstore.PersonalApp.Modules.Cvs.Features.Cvs;
using Dotnetstore.PersonalApp.Shared.Contacts;
using Xunit;

namespace Dotnetstore.PersonalApp.Modules.Cvs.Tests;

public sealed class CvContactTests : IDisposable
{
    private readonly CvsFixture _fixture = new();
    private readonly Guid _userId = Guid.NewGuid();

    public void Dispose() => _fixture.Dispose();

    private async Task<Guid> CreateCvAsync()
    {
        var created = await _fixture.Create.HandleAsync(CvsFixture.ValidCv(_userId), CancellationToken.None);
        return created.Value.Id;
    }

    [Theory]
    [InlineData("linkedin.com/in/anna", "https://linkedin.com/in/anna")]
    [InlineData("https://www.linkedin.com/in/anna/", "https://www.linkedin.com/in/anna/")]
    [InlineData("  se.linkedin.com/in/anna  ", "https://se.linkedin.com/in/anna")]
    [InlineData("", null)]
    [InlineData(null, null)]
    [InlineData("http://linkedin.com/in/anna", null)]
    [InlineData("https://notlinkedin.com/in/anna", null)]
    [InlineData("https://linkedin.com.evil.example/in/anna", null)]
    public void NormalizeLinkedInAcceptsOnlyLinkedInAddresses(string? input, string? expected) =>
        Assert.Equal(expected, CvRules.NormalizeLinkedIn(input));

    [Fact]
    public async Task LinkedInIsSavedNormalizedAndInvalidOnesAreRejected()
    {
        var created = await _fixture.Create.HandleAsync(CvsFixture.ValidCv(_userId) with { LinkedIn = "linkedin.com/in/anna" }, CancellationToken.None);
        Assert.Equal("https://linkedin.com/in/anna", created.Value.LinkedIn);

        var rejected = await _fixture.Update.HandleAsync(
            new UpdateCv.Command(created.Value.Id, _userId, "Backend developer", null, null, "https://example.com/anna"), CancellationToken.None);
        Assert.False(rejected.IsSuccess);
        Assert.Equal("cv_linkedin_invalid", rejected.Error.Code);

        var cleared = await _fixture.Update.HandleAsync(
            new UpdateCv.Command(created.Value.Id, _userId, "Backend developer", null, null, null), CancellationToken.None);
        Assert.Null(cleared.Value.LinkedIn);
    }

    [Fact]
    public async Task SelectedContactsAreReturnedWithTheDetail()
    {
        var cvId = await CreateCvAsync();
        var email = _fixture.Contacts.Add(_userId, ContactKind.Email, "Work", "anna@example.com");
        _fixture.Contacts.Add(_userId, ContactKind.Phone, "Mobile", "0701234567");

        var set = await _fixture.SetContacts.HandleAsync(new SetCvContacts.Command(cvId, _userId, [email.Id]), CancellationToken.None);
        Assert.True(set.IsSuccess);

        var detail = (await _fixture.GetDetail.HandleAsync(new GetCvDetail.Query(cvId, _userId), CancellationToken.None)).Value;
        Assert.Equal(2, detail.AvailableContacts.Count);
        Assert.Equal([email.Id], detail.SelectedContactIds);
        Assert.Equal("anna@example.com", Assert.Single(detail.SelectedContacts).Value);
    }

    [Fact]
    public async Task EntriesOfOtherUsersCannotBeSelected()
    {
        var cvId = await CreateCvAsync();
        var foreign = _fixture.Contacts.Add(Guid.NewGuid(), ContactKind.Email, "Work", "other@example.com");

        var set = await _fixture.SetContacts.HandleAsync(new SetCvContacts.Command(cvId, _userId, [foreign.Id, Guid.NewGuid()]), CancellationToken.None);

        Assert.True(set.IsSuccess);
        Assert.Empty(set.Value);
    }

    [Fact]
    public async Task SavingReplacesThePreviousSelection()
    {
        var cvId = await CreateCvAsync();
        var first = _fixture.Contacts.Add(_userId, ContactKind.Address, "Home", "Storgatan 1, 111 22 Stockholm");
        var second = _fixture.Contacts.Add(_userId, ContactKind.Email, "Private", "anna@example.com");

        await _fixture.SetContacts.HandleAsync(new SetCvContacts.Command(cvId, _userId, [first.Id, second.Id]), CancellationToken.None);
        await _fixture.SetContacts.HandleAsync(new SetCvContacts.Command(cvId, _userId, [second.Id]), CancellationToken.None);

        var detail = (await _fixture.GetDetail.HandleAsync(new GetCvDetail.Query(cvId, _userId), CancellationToken.None)).Value;
        Assert.Equal([second.Id], detail.SelectedContactIds);
    }

    [Fact]
    public async Task AnotherUsersCvIsNotFound()
    {
        var cvId = await CreateCvAsync();

        var set = await _fixture.SetContacts.HandleAsync(new SetCvContacts.Command(cvId, Guid.NewGuid(), []), CancellationToken.None);

        Assert.False(set.IsSuccess);
        Assert.Equal("cv_not_found", set.Error.Code);
    }

    [Fact]
    public async Task DetailViewModelShowsChosenContactsAndLinkedIn()
    {
        var created = await _fixture.Create.HandleAsync(CvsFixture.ValidCv(_userId) with { LinkedIn = "linkedin.com/in/anna" }, CancellationToken.None);
        var phone = _fixture.Contacts.Add(_userId, ContactKind.Phone, "Mobile", "0701234567");
        await _fixture.SetContacts.HandleAsync(new SetCvContacts.Command(created.Value.Id, _userId, [phone.Id]), CancellationToken.None);

        var viewModel = new CvDetailViewModel(created.Value, _userId, _fixture.DetailServices);
        await viewModel.LoadAsync(CancellationToken.None);

        Assert.Equal(["Mobile: 0701234567", "LinkedIn: https://linkedin.com/in/anna"], viewModel.ContactLines);
        Assert.False(viewModel.HasNoContact);
    }

    [Fact]
    public async Task WordExportIncludesTheChosenContacts()
    {
        var cvId = await CreateCvAsync();
        var email = _fixture.Contacts.Add(_userId, ContactKind.Email, "Work", "anna@example.com");
        await _fixture.SetContacts.HandleAsync(new SetCvContacts.Command(cvId, _userId, [email.Id]), CancellationToken.None);

        var export = await _fixture.Export.HandleAsync(new ExportCvToWord.Query(cvId, _userId), CancellationToken.None);

        Assert.True(export.IsSuccess);
        using var zip = new System.IO.Compression.ZipArchive(new MemoryStream(export.Value.Content));
        await using var entry = await zip.GetEntry("word/document.xml")!.OpenAsync(TestContext.Current.CancellationToken);
        using var reader = new StreamReader(entry);
        Assert.Contains("anna@example.com", await reader.ReadToEndAsync(TestContext.Current.CancellationToken), StringComparison.Ordinal);
    }
}
