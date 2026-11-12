using Dotnetstore.PersonalApp.Modules.Contact.PhoneNumbers.Features.PhoneNumberTypes;
using Dotnetstore.PersonalApp.Modules.Contact.Resources;
using Dotnetstore.PersonalApp.Shared.Localization;

namespace Dotnetstore.PersonalApp.Modules.Contact.Tests.PhoneNumbers;

public sealed class PhoneNumberTypesViewModelTests : IDisposable
{
    private readonly PhoneNumbersFixture _fixture = new();
    private readonly Localizer _localizer = new();
    private readonly CreatePhoneNumberType.Handler _create;
    private readonly UpdatePhoneNumberType.Handler _update;
    private readonly ListPhoneNumberTypes.Handler _list;
    private readonly DeletePhoneNumberType.Handler _delete;

    public PhoneNumberTypesViewModelTests()
    {
        _localizer.AddResources(ContactStrings.ResourceManager);
        _localizer.SetCulture("en");
        _create = new CreatePhoneNumberType.Handler(_fixture.DbFactory, new CreatePhoneNumberType.Validator(), _fixture.Clock);
        _update = new UpdatePhoneNumberType.Handler(_fixture.DbFactory, new UpdatePhoneNumberType.Validator());
        _list = new ListPhoneNumberTypes.Handler(_fixture.DbFactory);
        _delete = new DeletePhoneNumberType.Handler(_fixture.DbFactory);
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

    private PhoneNumberTypesViewModel List() => new(_list, _create, _update, _delete, _localizer);

    private PhoneNumberTypeEditorViewModel Editor(PhoneNumberTypeDto? existing = null) => new(existing, _create, _update, _localizer);

    private async Task<PhoneNumberTypeDto> AddAsync(string name) => (await _create.HandleAsync(new CreatePhoneNumberType.Command(name, null), Ct)).Value;

    [Fact]
    public async Task ANewEditorIsEmptyAndAnExistingOneIsFilled()
    {
        var fresh = Editor();
        fresh.Title.ShouldBe(_localizer.Get("PhoneNumberTypeEditor_New"));
        fresh.Name.ShouldBeEmpty();

        var existing = Editor(await AddAsync("Alpha"));
        existing.Title.ShouldBe(_localizer.Get("PhoneNumberTypeEditor_Edit"));
        existing.Name.ShouldBe("Alpha");
    }

    [Fact]
    public async Task SavingNewAndExistingRaiseSaved()
    {
        var editor = Editor();
        editor.Name = "Alpha";
        PhoneNumberTypeDto? saved = null;
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

        editor.ErrorMessage.ShouldBe(_localizer.Get("Error_phone_number_type_name_required"));
        raised.ShouldBeFalse();
    }

    [Fact]
    public async Task ADuplicateNameIsRejectedAndTheErrorClearsOnTheNextGoodSave()
    {
        await AddAsync("Alpha");
        var editor = Editor();
        editor.Name = "Alpha";

        await editor.SaveCommand.ExecuteAsync(null);
        editor.ErrorMessage.ShouldBe(_localizer.Get("Error_phone_number_type_conflict"));

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
        var baseline = list.PhoneNumberTypes.Count;
        await AddAsync("Alpha");
        await AddAsync("Beta");

        await list.LoadCommand.ExecuteAsync(null);
        list.PhoneNumberTypes.Count.ShouldBe(baseline + 2);
        list.StatusMessage.ShouldBe(_localizer.Format("PhoneNumberTypes_Count", baseline + 2));

        list.Search = "Bet";
        await list.LoadCommand.ExecuteAsync(null);
        list.PhoneNumberTypes.Single().Name.ShouldBe("Beta");
    }

    [Fact]
    public async Task SelectingAndNewOpenTheEditorAndCancelClosesIt()
    {
        await AddAsync("Alpha");
        var list = List();
        await list.LoadCommand.ExecuteAsync(null);

        list.SelectedType = list.PhoneNumberTypes.Single(x => x.Name == "Alpha");
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

        await WaitUntilAsync(() => list.StatusMessage == _localizer.Format("PhoneNumberTypes_Saved", "Gamma"));
        list.Editor.ShouldBeNull();
        list.PhoneNumberTypes.ShouldContain(x => x.Name == "Gamma");
    }

    [Fact]
    public async Task DeleteNeedsConfirmationAndCanBeWithdrawn()
    {
        await AddAsync("Alpha");
        var list = List();
        await list.LoadCommand.ExecuteAsync(null);
        list.RequestDeleteCommand.CanExecute(null).ShouldBeFalse();
        list.SelectedType = list.PhoneNumberTypes.Single(x => x.Name == "Alpha");

        list.RequestDeleteCommand.Execute(null);
        list.IsConfirmingDelete.ShouldBeTrue();
        list.CancelDeleteCommand.Execute(null);

        list.IsConfirmingDelete.ShouldBeFalse();
        list.PhoneNumberTypes.ShouldContain(x => x.Name == "Alpha");
    }

    [Fact]
    public async Task ConfirmingTheDeleteRemovesTheItem()
    {
        await AddAsync("Alpha");
        var list = List();
        await list.LoadCommand.ExecuteAsync(null);
        list.SelectedType = list.PhoneNumberTypes.Single(x => x.Name == "Alpha");
        list.RequestDeleteCommand.Execute(null);

        await list.ConfirmDeleteCommand.ExecuteAsync(null);

        list.PhoneNumberTypes.ShouldNotContain(x => x.Name == "Alpha");
        list.Editor.ShouldBeNull();
        list.IsConfirmingDelete.ShouldBeFalse();
    }

    [Fact]
    public async Task DeletingSomethingAlreadyGoneShowsTheError()
    {
        var item = await AddAsync("Alpha");
        var list = List();
        await list.LoadCommand.ExecuteAsync(null);
        list.SelectedType = list.PhoneNumberTypes.Single(x => x.Name == "Alpha");
        await _delete.HandleAsync(new DeletePhoneNumberType.Command(item.Id), Ct);

        await list.ConfirmDeleteCommand.ExecuteAsync(null);

        list.StatusMessage.ShouldBe(_localizer.Get("Error_phone_number_type_not_found"));
    }
}
