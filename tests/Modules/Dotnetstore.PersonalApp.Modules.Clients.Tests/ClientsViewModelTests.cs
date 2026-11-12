using Dotnetstore.PersonalApp.Modules.Clients.Features.Clients;
using Dotnetstore.PersonalApp.Modules.Clients.Features.ContactPersons;
using Dotnetstore.PersonalApp.Modules.Clients.Resources;
using Dotnetstore.PersonalApp.Shared.Localization;

namespace Dotnetstore.PersonalApp.Modules.Clients.Tests;

public sealed class ClientsViewModelTests : IDisposable
{
    private readonly ClientsFixture _fixture = new();
    private readonly Localizer _localizer = new();

    public ClientsViewModelTests()
    {
        _localizer.AddResources(ClientsStrings.ResourceManager);
        _localizer.SetCulture("en");
    }

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public void Dispose() => _fixture.Dispose();

    private static async Task WaitUntilAsync(Func<bool> condition)
    {
        for (var i = 0; i < 500 && !condition(); i++)
        {
            await Task.Delay(10, Ct);
        }

        condition().ShouldBeTrue();
    }

    private string Error(string code) => _localizer.Get("Error_" + code);

    private ClientsViewModel List() =>
        new(
            _fixture.List,
            _fixture.Create,
            _fixture.Update,
            _fixture.Delete,
            _localizer,
            new ContactPersonsViewModelFactory(_fixture.ListContactPersons, _fixture.CreateContactPerson, _fixture.UpdateContactPerson, _fixture.DeleteContactPerson, _localizer));

    private ClientEditorViewModel Editor(ClientDto? existing = null) => new(existing, _fixture.Create, _fixture.Update, _localizer);

    private async Task<ClientDto> AddAsync(string name = "Acme AB", string organizationNumber = "556677-8899") =>
        (await _fixture.Create.HandleAsync(ClientsFixture.ValidCommand(organizationNumber, name), Ct)).Value;

    private static void Fill(ClientEditorViewModel editor, string name = "Acme AB", string organizationNumber = "556677-8899")
    {
        editor.Name = name;
        editor.OrganizationNumber = organizationNumber;
    }

    // ---- editor ----

    [Fact]
    public async Task ANewEditorIsEmptyAndAnExistingOneIsFilled()
    {
        var fresh = Editor();
        fresh.Title.ShouldBe(_localizer.Get("ClientEditor_New"));
        fresh.Name.ShouldBeEmpty();

        var client = await AddAsync();
        var existing = Editor(client);
        existing.Title.ShouldBe(_localizer.Get("ClientEditor_Edit"));
        existing.Name.ShouldBe("Acme AB");
        existing.OrganizationNumber.ShouldBe(client.OrganizationNumber);
        existing.Email.ShouldBe("info@acme.example");
        existing.Website.ShouldBe("https://acme.example");
    }

    [Fact]
    public async Task SavingANewClientRaisesSaved()
    {
        var editor = Editor();
        Fill(editor);
        ClientDto? saved = null;
        editor.Saved += (_, dto) => saved = dto;

        await editor.SaveCommand.ExecuteAsync(null);

        saved.ShouldNotBeNull().Name.ShouldBe("Acme AB");
        editor.ErrorMessage.ShouldBeNull();
    }

    [Fact]
    public async Task SavingAnExistingClientUpdatesItInPlace()
    {
        var client = await AddAsync();
        var editor = Editor(client);
        editor.Name = "Acme Group";
        ClientDto? saved = null;
        editor.Saved += (_, dto) => saved = dto;

        await editor.SaveCommand.ExecuteAsync(null);

        saved.ShouldNotBeNull().Id.ShouldBe(client.Id);
        saved.Name.ShouldBe("Acme Group");
    }

    [Fact]
    public async Task ABlankNameAndOrganizationNumberAreRejected()
    {
        var editor = Editor();
        var raised = false;
        editor.Saved += (_, _) => raised = true;

        await editor.SaveCommand.ExecuteAsync(null);
        editor.ErrorMessage.ShouldBe(Error("client_name_required"));

        editor.Name = "Acme AB";
        await editor.SaveCommand.ExecuteAsync(null);
        editor.ErrorMessage.ShouldBe(Error("client_organization_number_required"));
        raised.ShouldBeFalse();
    }

    [Fact]
    public async Task AnInvalidOrganizationNumberIsRejected()
    {
        var editor = Editor();
        Fill(editor, organizationNumber: "12 34!");

        await editor.SaveCommand.ExecuteAsync(null);

        editor.ErrorMessage.ShouldBe(Error("client_organization_number_invalid"));
    }

    [Fact]
    public async Task AnInvalidEmailAndWebsiteAreRejected()
    {
        var editor = Editor();
        Fill(editor);
        editor.Email = "not-an-email";
        await editor.SaveCommand.ExecuteAsync(null);
        editor.ErrorMessage.ShouldBe(Error("client_email_invalid"));

        editor.Email = string.Empty;
        editor.Website = "ftp://nope";
        await editor.SaveCommand.ExecuteAsync(null);
        editor.ErrorMessage.ShouldBe(Error("client_website_invalid"));
    }

    [Fact]
    public async Task ADuplicateOrganizationNumberIsRejectedAndTheErrorClearsOnTheNextGoodSave()
    {
        await AddAsync();
        var editor = Editor();
        Fill(editor, "Other AB");

        await editor.SaveCommand.ExecuteAsync(null);
        editor.ErrorMessage.ShouldBe(Error("client_conflict"));

        editor.OrganizationNumber = "112233-4455";
        await editor.SaveCommand.ExecuteAsync(null);
        editor.ErrorMessage.ShouldBeNull();
    }

    [Fact]
    public async Task CancelRaisesCancelled()
    {
        var editor = Editor();
        var cancelled = false;
        editor.Cancelled += (_, _) => cancelled = true;

        editor.CancelCommand.Execute(null);

        cancelled.ShouldBeTrue();
    }

    // ---- list ----

    [Fact]
    public async Task TheListLoadsAndSearches()
    {
        await AddAsync("Alpha AB");
        await AddAsync("Beta AB", "112233-4455");
        var list = List();

        await list.LoadCommand.ExecuteAsync(null);
        list.Clients.Count.ShouldBe(2);
        list.StatusMessage.ShouldBe(_localizer.Format("Clients_Count", 2));

        list.Search = "Beta";
        await list.LoadCommand.ExecuteAsync(null);
        list.Clients.Single().Name.ShouldBe("Beta AB");
    }

    [Fact]
    public async Task ReloadingKeepsTheSelectionWithoutReopeningTheEditor()
    {
        await AddAsync();
        var list = List();
        await list.LoadCommand.ExecuteAsync(null);
        list.SelectedClient = list.Clients.Single();
        var editor = list.Editor;

        await list.LoadCommand.ExecuteAsync(null);

        list.SelectedClient.ShouldNotBeNull().Name.ShouldBe("Acme AB");
        list.Editor.ShouldBeSameAs(editor);
    }

    [Fact]
    public async Task SelectingAndNewOpenTheEditorAndCancelClosesIt()
    {
        await AddAsync();
        var list = List();
        await list.LoadCommand.ExecuteAsync(null);

        list.SelectedClient = list.Clients.Single();
        list.Editor.ShouldNotBeNull().Name.ShouldBe("Acme AB");

        list.NewCommand.Execute(null);
        list.SelectedClient.ShouldBeNull();
        list.Editor.ShouldNotBeNull().Name.ShouldBeEmpty();

        list.Editor.CancelCommand.Execute(null);
        list.Editor.ShouldBeNull();
    }

    [Fact]
    public async Task SavingFromTheEditorClosesItAndReloads()
    {
        var list = List();
        list.NewCommand.Execute(null);
        Fill(list.Editor!, "Gamma AB", "112233-4455");

        await list.Editor!.SaveCommand.ExecuteAsync(null);

        await WaitUntilAsync(() => list.StatusMessage == _localizer.Format("Clients_Saved", "Gamma AB"));
        list.Editor.ShouldBeNull();
        list.Clients.Single().Name.ShouldBe("Gamma AB");
    }

    [Fact]
    public async Task DetailOpensForTheSelectionAndBackReturnsToTheList()
    {
        await AddAsync();
        var list = List();
        list.OpenDetailCommand.CanExecute(null).ShouldBeFalse();
        await list.LoadCommand.ExecuteAsync(null);
        list.SelectedClient = list.Clients.Single();

        list.OpenDetailCommand.Execute(null);
        list.Detail.ShouldNotBeNull();

        list.Detail.BackCommand.Execute(null);
        list.Detail.ShouldBeNull();
    }

    [Fact]
    public async Task DeleteNeedsConfirmationAndCanBeWithdrawn()
    {
        await AddAsync();
        var list = List();
        list.RequestDeleteCommand.CanExecute(null).ShouldBeFalse();
        await list.LoadCommand.ExecuteAsync(null);
        list.SelectedClient = list.Clients.Single();

        list.RequestDeleteCommand.Execute(null);
        list.IsConfirmingDelete.ShouldBeTrue();
        list.CancelDeleteCommand.Execute(null);

        list.IsConfirmingDelete.ShouldBeFalse();
        list.Clients.Count.ShouldBe(1);
    }

    [Fact]
    public async Task ConfirmingTheDeleteRemovesTheClientAndTellsTheOtherModules()
    {
        var client = await AddAsync();
        var list = List();
        await list.LoadCommand.ExecuteAsync(null);
        list.SelectedClient = list.Clients.Single();
        list.RequestDeleteCommand.Execute(null);

        await list.ConfirmDeleteCommand.ExecuteAsync(null);

        list.Clients.ShouldBeEmpty();
        list.Editor.ShouldBeNull();
        list.IsConfirmingDelete.ShouldBeFalse();
        _fixture.Events.Published.Count.ShouldBe(1);
        client.Id.ShouldNotBe(Guid.Empty);
    }

    [Fact]
    public async Task DeletingAClientThatIsAlreadyGoneShowsTheError()
    {
        var client = await AddAsync();
        var list = List();
        await list.LoadCommand.ExecuteAsync(null);
        list.SelectedClient = list.Clients.Single();
        await _fixture.Delete.HandleAsync(new DeleteClient.Command(client.Id), Ct);

        await list.ConfirmDeleteCommand.ExecuteAsync(null);

        list.StatusMessage.ShouldBe(Error("client_not_found"));
    }

    // ---- contact persons ----

    private ContactPersonsViewModel People(Guid clientId) =>
        new(clientId, _fixture.ListContactPersons, _fixture.CreateContactPerson, _fixture.UpdateContactPerson, _fixture.DeleteContactPerson, _localizer);

    private ContactPersonEditorViewModel PersonEditor(Guid clientId, ContactPersonDto? existing = null) =>
        new(clientId, existing, _fixture.CreateContactPerson, _fixture.UpdateContactPerson, _localizer);

    private async Task<ContactPersonDto> AddPersonAsync(Guid clientId, string first = "Anna", string last = "Andersson") =>
        (await _fixture.CreateContactPerson.HandleAsync(new CreateContactPerson.Command(clientId, first, last, "CEO", null), Ct)).Value;

    [Fact]
    public async Task APersonEditorSavesNewAndExistingPeopleAndRejectsMissingNames()
    {
        var client = await AddAsync();
        var editor = PersonEditor(client.Id);
        editor.Title.ShouldBe(_localizer.Get("ContactPersonEditor_New"));
        ContactPersonDto? saved = null;
        editor.Saved += (_, dto) => saved = dto;

        await editor.SaveCommand.ExecuteAsync(null);
        editor.ErrorMessage.ShouldBe(Error("contact_person_first_name_required"));
        editor.FirstName = "Anna";
        await editor.SaveCommand.ExecuteAsync(null);
        editor.ErrorMessage.ShouldBe(Error("contact_person_last_name_required"));
        editor.LastName = "Andersson";
        await editor.SaveCommand.ExecuteAsync(null);
        editor.ErrorMessage.ShouldBeNull();
        saved.ShouldNotBeNull().FullName.ShouldBe("Anna Andersson");

        var again = PersonEditor(client.Id, saved);
        again.Title.ShouldBe(_localizer.Get("ContactPersonEditor_Edit"));
        again.JobTitle.ShouldBe(string.Empty);
        again.LastName = "Svensson";
        again.Saved += (_, dto) => saved = dto;
        await again.SaveCommand.ExecuteAsync(null);
        saved.LastName.ShouldBe("Svensson");
    }

    [Fact]
    public async Task APersonForAnUnknownClientIsRejected()
    {
        var editor = PersonEditor(Guid.NewGuid());
        editor.FirstName = "Anna";
        editor.LastName = "Andersson";

        await editor.SaveCommand.ExecuteAsync(null);

        editor.ErrorMessage.ShouldBe(Error("client_not_found"));
    }

    [Fact]
    public async Task APersonEditorCancelRaisesCancelled()
    {
        var editor = PersonEditor(Guid.NewGuid());
        var cancelled = false;
        editor.Cancelled += (_, _) => cancelled = true;

        editor.CancelCommand.Execute(null);

        cancelled.ShouldBeTrue();
    }

    [Fact]
    public async Task ThePeopleListShowsOnlyThisClientsPeople()
    {
        var client = await AddAsync();
        var other = await AddAsync("Other AB", "112233-4455");
        await AddPersonAsync(client.Id);
        await AddPersonAsync(other.Id, "Bo", "Berg");
        var people = People(client.Id);

        await people.LoadCommand.ExecuteAsync(null);

        people.People.Single().FullName.ShouldBe("Anna Andersson");
        people.StatusMessage.ShouldBe(_localizer.Format("ContactPersons_Count", 1));
    }

    [Fact]
    public async Task PeopleSelectNewSaveAndCancelWork()
    {
        var client = await AddAsync();
        await AddPersonAsync(client.Id);
        var people = People(client.Id);
        await people.LoadCommand.ExecuteAsync(null);

        people.SelectedPerson = people.People.Single();
        people.Editor.ShouldNotBeNull().FirstName.ShouldBe("Anna");
        var editor = people.Editor;
        await people.LoadCommand.ExecuteAsync(null);
        people.Editor.ShouldBeSameAs(editor);

        people.NewCommand.Execute(null);
        people.SelectedPerson.ShouldBeNull();
        people.Editor!.FirstName = "Bo";
        people.Editor.LastName = "Berg";
        await people.Editor.SaveCommand.ExecuteAsync(null);

        await WaitUntilAsync(() => people.StatusMessage == _localizer.Format("ContactPersons_Saved", "Bo Berg"));
        people.Editor.ShouldBeNull();
        people.People.Count.ShouldBe(2);

        people.NewCommand.Execute(null);
        people.Editor!.CancelCommand.Execute(null);
        people.Editor.ShouldBeNull();
    }

    [Fact]
    public async Task APersonDetailOpensAndBackReturns()
    {
        var client = await AddAsync();
        await AddPersonAsync(client.Id);
        var people = People(client.Id);
        people.OpenDetailCommand.CanExecute(null).ShouldBeFalse();
        await people.LoadCommand.ExecuteAsync(null);
        people.SelectedPerson = people.People.Single();

        people.OpenDetailCommand.Execute(null);
        people.Detail.ShouldNotBeNull().Person.FullName.ShouldBe("Anna Andersson");
        people.Detail.Photo.ShouldBeNull();
        people.Detail.Addresses.ShouldBeNull();

        people.Detail.BackCommand.Execute(null);
        people.Detail.ShouldBeNull();
    }

    [Fact]
    public async Task DeletingAPersonNeedsConfirmationAndShowsTheErrorWhenTheyAreAlreadyGone()
    {
        var client = await AddAsync();
        var person = await AddPersonAsync(client.Id);
        await AddPersonAsync(client.Id, "Bo", "Berg");
        var people = People(client.Id);
        await people.LoadCommand.ExecuteAsync(null);
        people.SelectedPerson = people.People.Single(p => p.Id == person.Id);

        people.RequestDeleteCommand.Execute(null);
        people.IsConfirmingDelete.ShouldBeTrue();
        people.CancelDeleteCommand.Execute(null);
        people.IsConfirmingDelete.ShouldBeFalse();

        await _fixture.DeleteContactPerson.HandleAsync(new DeleteContactPerson.Command(person.Id), Ct);
        await people.ConfirmDeleteCommand.ExecuteAsync(null);
        people.StatusMessage.ShouldBe(Error("contact_person_not_found"));

        people.People.Single().FirstName.ShouldBe("Bo");
        people.SelectedPerson = people.People.Single();
        await people.ConfirmDeleteCommand.ExecuteAsync(null);
        people.People.ShouldBeEmpty();
    }
}
