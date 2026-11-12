using Dotnetstore.PersonalApp.Modules.Contact.Addresses.Features.AddressTypes;
using Dotnetstore.PersonalApp.Modules.Contact.Resources;
using Dotnetstore.PersonalApp.Shared.Localization;

namespace Dotnetstore.PersonalApp.Modules.Contact.Tests.Addresses;

public sealed class AddressTypesViewModelTests : IDisposable
{
    private readonly AddressesFixture _fixture = new();
    private readonly Localizer _localizer = new();
    private readonly CreateAddressType.Handler _create;
    private readonly UpdateAddressType.Handler _update;
    private readonly ListAddressTypes.Handler _list;
    private readonly DeleteAddressType.Handler _delete;

    public AddressTypesViewModelTests()
    {
        _localizer.AddResources(ContactStrings.ResourceManager);
        _localizer.SetCulture("en");
        _create = new CreateAddressType.Handler(_fixture.DbFactory, new CreateAddressType.Validator(), _fixture.Clock);
        _update = new UpdateAddressType.Handler(_fixture.DbFactory, new UpdateAddressType.Validator());
        _list = new ListAddressTypes.Handler(_fixture.DbFactory);
        _delete = new DeleteAddressType.Handler(_fixture.DbFactory);
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

    private AddressTypesViewModel List() => new(_list, _create, _update, _delete, _localizer);

    private AddressTypeEditorViewModel Editor(AddressTypeDto? existing = null) => new(existing, _create, _update, _localizer);

    private async Task<AddressTypeDto> AddAsync(string name) => (await _create.HandleAsync(new CreateAddressType.Command(name, null), Ct)).Value;

    [Fact]
    public async Task ANewEditorIsEmptyAndAnExistingOneIsFilled()
    {
        var fresh = Editor();
        fresh.Title.ShouldBe(_localizer.Get("AddressTypeEditor_New"));
        fresh.Name.ShouldBeEmpty();

        var existing = Editor(await AddAsync("Alpha"));
        existing.Title.ShouldBe(_localizer.Get("AddressTypeEditor_Edit"));
        existing.Name.ShouldBe("Alpha");
    }

    [Fact]
    public async Task SavingNewAndExistingRaiseSaved()
    {
        var editor = Editor();
        editor.Name = "Alpha";
        AddressTypeDto? saved = null;
        editor.Saved += (_, dto) => saved = dto;
        await editor.SaveCommand.ExecuteAsync(null);
        var id = saved.ShouldNotBeNull().Id;

        var again = Editor(saved);
        again.Name = "Beta";
        again.Saved += (_, dto) => saved = dto;
        await again.SaveCommand.ExecuteAsync(null);

        saved.ShouldNotBeNull().Id.ShouldBe(id);
        saved.Name.ShouldBe("Beta");
        editor.ErrorMessage.ShouldBeNull();
    }

    [Fact]
    public async Task ABlankNameIsRejectedAndNothingIsRaised()
    {
        var editor = Editor();
        var raised = false;
        editor.Saved += (_, _) => raised = true;

        await editor.SaveCommand.ExecuteAsync(null);

        editor.ErrorMessage.ShouldBe(_localizer.Get("Error_address_type_name_required"));
        raised.ShouldBeFalse();
    }

    [Fact]
    public async Task ADuplicateNameIsRejectedAndTheErrorClearsOnTheNextGoodSave()
    {
        await AddAsync("Alpha");
        var editor = Editor();
        editor.Name = "Alpha";

        await editor.SaveCommand.ExecuteAsync(null);
        editor.ErrorMessage.ShouldBe(_localizer.Get("Error_address_type_conflict"));

        editor.Name = "Beta";
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

    [Fact]
    public async Task TheListLoadsAndSearches()
    {
        var list = List();
        await list.LoadCommand.ExecuteAsync(null);
        var baseline = list.AddressTypes.Count;
        await AddAsync("Alpha");
        await AddAsync("Beta");

        await list.LoadCommand.ExecuteAsync(null);
        list.AddressTypes.Count.ShouldBe(baseline + 2);
        list.StatusMessage.ShouldBe(_localizer.Format("AddressTypes_Count", baseline + 2));

        list.Search = "Bet";
        await list.LoadCommand.ExecuteAsync(null);
        list.AddressTypes.Single().Name.ShouldBe("Beta");
    }

    [Fact]
    public async Task SelectingAndNewOpenTheEditorAndCancelClosesIt()
    {
        await AddAsync("Alpha");
        var list = List();
        await list.LoadCommand.ExecuteAsync(null);

        list.SelectedType = list.AddressTypes.Single(x => x.Name == "Alpha");
        list.Editor.ShouldNotBeNull().Name.ShouldBe("Alpha");

        list.NewCommand.Execute(null);
        list.SelectedType.ShouldBeNull();
        list.Editor.ShouldNotBeNull().Name.ShouldBeEmpty();

        list.Editor.CancelCommand.Execute(null);
        list.Editor.ShouldBeNull();
    }

    [Fact]
    public async Task SavingFromTheEditorClosesItAndReloads()
    {
        var list = List();
        list.NewCommand.Execute(null);
        list.Editor!.Name = "Gamma";

        await list.Editor.SaveCommand.ExecuteAsync(null);

        await WaitUntilAsync(() => list.StatusMessage == _localizer.Format("AddressTypes_Saved", "Gamma"));
        list.Editor.ShouldBeNull();
        list.AddressTypes.ShouldContain(x => x.Name == "Gamma");
    }

    [Fact]
    public async Task DeleteNeedsConfirmationAndCanBeWithdrawn()
    {
        await AddAsync("Alpha");
        var list = List();
        await list.LoadCommand.ExecuteAsync(null);
        list.RequestDeleteCommand.CanExecute(null).ShouldBeFalse();
        list.SelectedType = list.AddressTypes.Single(x => x.Name == "Alpha");

        list.RequestDeleteCommand.Execute(null);
        list.IsConfirmingDelete.ShouldBeTrue();
        list.CancelDeleteCommand.Execute(null);

        list.IsConfirmingDelete.ShouldBeFalse();
        list.AddressTypes.ShouldContain(x => x.Name == "Alpha");
    }

    [Fact]
    public async Task ConfirmingTheDeleteRemovesTheItem()
    {
        await AddAsync("Alpha");
        var list = List();
        await list.LoadCommand.ExecuteAsync(null);
        list.SelectedType = list.AddressTypes.Single(x => x.Name == "Alpha");
        list.RequestDeleteCommand.Execute(null);

        await list.ConfirmDeleteCommand.ExecuteAsync(null);

        list.AddressTypes.ShouldNotContain(x => x.Name == "Alpha");
        list.Editor.ShouldBeNull();
        list.IsConfirmingDelete.ShouldBeFalse();
    }

    [Fact]
    public async Task DeletingSomethingAlreadyGoneShowsTheError()
    {
        var item = await AddAsync("Alpha");
        var list = List();
        await list.LoadCommand.ExecuteAsync(null);
        list.SelectedType = list.AddressTypes.Single(x => x.Name == "Alpha");
        await _delete.HandleAsync(new DeleteAddressType.Command(item.Id), Ct);

        await list.ConfirmDeleteCommand.ExecuteAsync(null);

        list.StatusMessage.ShouldBe(_localizer.Get("Error_address_type_not_found"));
    }
}
