using Dotnetstore.PersonalApp.Modules.Cvs.Features.Cvs;
using Dotnetstore.PersonalApp.Shared.Contacts;

namespace Dotnetstore.PersonalApp.Modules.Cvs.Tests;

public sealed class TechnologyTagsTests
{
    [Fact]
    public void SplitAcceptsCommasSemicolonsAndLineBreaks() =>
        TechnologyTags.Split("C#, .NET;SQL\nAvalonia\r\nGit").ShouldBe(["C#", ".NET", "SQL", "Avalonia", "Git"]);

    [Fact]
    public void BlankInputGivesNoTags()
    {
        TechnologyTags.Split(null).ShouldBeEmpty();
        TechnologyTags.Split("  ").ShouldBeEmpty();
    }

    [Fact]
    public void DuplicatesAreDroppedIgnoringCaseAndTheFirstSpellingWins() =>
        TechnologyTags.Normalize(["C#", "c#", " SQL ", "", "sql"]).ShouldBe(["C#", "SQL"]);

    [Fact]
    public void ATagNeverContainsACommaSoItCanBeStoredAsOneString()
    {
        var stored = TechnologyTags.Join(["A,B", "C"]);

        stored.ShouldBe("A,B,C");
        TechnologyTags.Parse(stored).ShouldBe(["A", "B", "C"]);
    }
}

public sealed class DriverLicenceCategoriesTests
{
    [Fact]
    public void JoinKeepsOnlyKnownCategoriesInCanonicalOrderWithoutDuplicates() =>
        DriverLicenceCategories.Join(["be", "B", "XX", "b", "AM"]).ShouldBe("AM,B,BE");

    [Fact]
    public void ParseReadsWhatJoinStored()
    {
        var stored = DriverLicenceCategories.Join(["C", "A"]);

        DriverLicenceCategories.Parse(stored).ShouldBe(["A", "C"]);
    }

    [Fact]
    public void NothingChosenIsAnEmptyString()
    {
        DriverLicenceCategories.Join([]).ShouldBeEmpty();
        DriverLicenceCategories.Parse(string.Empty).ShouldBeEmpty();
    }
}

public sealed class ContactEntriesTests
{
    [Fact]
    public async Task EntriesAreGroupedAddressesFirstThenPhonesThenEmailsAndSortedByTypeAndValue()
    {
        var entries = await ContactEntries.ListAsync(
            [
                new StubSource(Entry(ContactKind.Email, "Work", "b@x.se"), Entry(ContactKind.Phone, "Work", "070")),
                new StubSource(Entry(ContactKind.Address, "Work", "Street"), Entry(ContactKind.Email, "Home", "a@x.se")),
            ],
            Guid.NewGuid(),
            TestContext.Current.CancellationToken);

        entries.Select(e => e.Value).ShouldBe(["Street", "070", "a@x.se", "b@x.se"]);
    }

    [Fact]
    public async Task NoSourcesGiveNoEntries() =>
        (await ContactEntries.ListAsync([], Guid.NewGuid(), TestContext.Current.CancellationToken)).ShouldBeEmpty();

    private static ContactEntry Entry(ContactKind kind, string type, string value) => new(Guid.NewGuid(), kind, type, value);

    private sealed class StubSource(params ContactEntry[] entries) : IContactInfoSource
    {
        public Task<IReadOnlyList<ContactEntry>> ListAsync(Guid objectId, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<ContactEntry>>(entries);
    }
}
