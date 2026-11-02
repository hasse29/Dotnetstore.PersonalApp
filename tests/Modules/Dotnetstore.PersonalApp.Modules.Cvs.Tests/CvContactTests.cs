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
        CvRules.NormalizeLinkedIn(input).ShouldBe(expected);

    [Fact]
    public async Task LinkedInIsSavedNormalizedAndInvalidOnesAreRejected()
    {
        var created = await _fixture.Create.HandleAsync(CvsFixture.ValidCv(_userId) with { LinkedIn = "linkedin.com/in/anna" }, CancellationToken.None);
        created.Value.LinkedIn.ShouldBe("https://linkedin.com/in/anna");

        var rejected = await _fixture.Update.HandleAsync(
            new UpdateCv.Command(created.Value.Id, _userId, "Backend developer", null, null, "https://example.com/anna"), CancellationToken.None);
        rejected.IsSuccess.ShouldBeFalse();
        rejected.Error.Code.ShouldBe("cv_linkedin_invalid");

        var cleared = await _fixture.Update.HandleAsync(
            new UpdateCv.Command(created.Value.Id, _userId, "Backend developer", null, null, null), CancellationToken.None);
        cleared.Value.LinkedIn.ShouldBeNull();
    }

    [Fact]
    public async Task SelectedContactsAreReturnedWithTheDetail()
    {
        var cvId = await CreateCvAsync();
        var email = _fixture.Contacts.Add(_userId, ContactKind.Email, "Work", "anna@example.com");
        _fixture.Contacts.Add(_userId, ContactKind.Phone, "Mobile", "0701234567");

        var set = await _fixture.SetContacts.HandleAsync(new SetCvContacts.Command(cvId, _userId, [email.Id]), CancellationToken.None);
        set.IsSuccess.ShouldBeTrue();

        var detail = (await _fixture.GetDetail.HandleAsync(new GetCvDetail.Query(cvId, _userId), CancellationToken.None)).Value;
        detail.AvailableContacts.Count.ShouldBe(2);
        detail.SelectedContactIds.ShouldBe([email.Id]);
        detail.SelectedContacts.ShouldHaveSingleItem().Value.ShouldBe("anna@example.com");
    }

    [Fact]
    public async Task EntriesOfOtherUsersCannotBeSelected()
    {
        var cvId = await CreateCvAsync();
        var foreign = _fixture.Contacts.Add(Guid.NewGuid(), ContactKind.Email, "Work", "other@example.com");

        var set = await _fixture.SetContacts.HandleAsync(new SetCvContacts.Command(cvId, _userId, [foreign.Id, Guid.NewGuid()]), CancellationToken.None);

        set.IsSuccess.ShouldBeTrue();
        set.Value.ShouldBeEmpty();
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
        detail.SelectedContactIds.ShouldBe([second.Id]);
    }

    [Fact]
    public async Task AnotherUsersCvIsNotFound()
    {
        var cvId = await CreateCvAsync();

        var set = await _fixture.SetContacts.HandleAsync(new SetCvContacts.Command(cvId, Guid.NewGuid(), []), CancellationToken.None);

        set.IsSuccess.ShouldBeFalse();
        set.Error.Code.ShouldBe("cv_not_found");
    }

    [Fact]
    public async Task DetailViewModelShowsChosenContactsAndLinkedIn()
    {
        var created = await _fixture.Create.HandleAsync(CvsFixture.ValidCv(_userId) with { LinkedIn = "linkedin.com/in/anna" }, CancellationToken.None);
        var phone = _fixture.Contacts.Add(_userId, ContactKind.Phone, "Mobile", "0701234567");
        await _fixture.SetContacts.HandleAsync(new SetCvContacts.Command(created.Value.Id, _userId, [phone.Id]), CancellationToken.None);

        var viewModel = new CvDetailViewModel(created.Value, _userId, _fixture.DetailServices);
        await viewModel.LoadAsync(CancellationToken.None);

        viewModel.ContactLines.ShouldBe(["Mobile: 0701234567", "LinkedIn: https://linkedin.com/in/anna"]);
        viewModel.HasNoContact.ShouldBeFalse();
    }

    [Fact]
    public async Task WordExportIncludesTheChosenContacts()
    {
        var cvId = await CreateCvAsync();
        var email = _fixture.Contacts.Add(_userId, ContactKind.Email, "Work", "anna@example.com");
        await _fixture.SetContacts.HandleAsync(new SetCvContacts.Command(cvId, _userId, [email.Id]), CancellationToken.None);

        var export = await _fixture.Export.HandleAsync(new ExportCvToWord.Query(cvId, _userId), CancellationToken.None);

        export.IsSuccess.ShouldBeTrue();
        using var zip = new System.IO.Compression.ZipArchive(new MemoryStream(export.Value.Content));
        await using var entry = await zip.GetEntry("word/document.xml")!.OpenAsync(TestContext.Current.CancellationToken);
        using var reader = new StreamReader(entry);
        (await reader.ReadToEndAsync(TestContext.Current.CancellationToken)).ShouldContain("anna@example.com", Case.Sensitive);
    }
}
