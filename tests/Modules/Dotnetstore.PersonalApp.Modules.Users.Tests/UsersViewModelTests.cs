using Dotnetstore.PersonalApp.Modules.Users.Features.Users;
using Dotnetstore.PersonalApp.Modules.Users.Resources;
using Dotnetstore.PersonalApp.Shared.Localization;

namespace Dotnetstore.PersonalApp.Modules.Users.Tests;

public sealed class UsersViewModelTests : IDisposable
{
    private readonly UsersFixture _fixture = new();
    private readonly Localizer _localizer = new();

    public UsersViewModelTests()
    {
        _localizer.AddResources(UsersStrings.ResourceManager);
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

    private UsersViewModel List() => new(_fixture.List, _fixture.Create, _fixture.Update, _fixture.Delete, _localizer);

    private UserEditorViewModel Editor(UserDto? existing = null) => new(existing, _fixture.Create, _fixture.Update, _localizer);

    private async Task<UserDto> AddAsync(string username = "ada@example.com") =>
        (await _fixture.Create.HandleAsync(UsersFixture.ValidCommand(username), Ct)).Value;

    private static void Fill(UserEditorViewModel editor, string username = "ada@example.com")
    {
        editor.FirstName = "Ada";
        editor.LastName = "Lovelace";
        editor.Username = username;
        editor.Password = "correct horse battery";
        editor.DateOfBirth = new DateTime(1990, 12, 10);
    }

    [Fact]
    public async Task ANewEditorAsksForAPasswordAndAnExistingOneKeepsIt()
    {
        var fresh = Editor();
        fresh.IsNew.ShouldBeTrue();
        fresh.Title.ShouldBe(_localizer.Get("Editor_New"));
        fresh.PasswordPlaceholder.ShouldBe(_localizer.Get("Field_Password"));

        var existing = Editor(await AddAsync());
        existing.IsNew.ShouldBeFalse();
        existing.Title.ShouldBe(_localizer.Get("Editor_Edit"));
        existing.PasswordPlaceholder.ShouldBe(_localizer.Get("Editor_PasswordKeep"));
        existing.Username.ShouldBe("ada@example.com");
        existing.Password.ShouldBeEmpty();
        existing.DateOfBirth.ShouldBe(new DateTime(1990, 12, 10));
    }

    [Fact]
    public async Task SavingANewUserRaisesSaved()
    {
        var editor = Editor();
        Fill(editor);
        UserDto? saved = null;
        editor.Saved += (_, dto) => saved = dto;

        await editor.SaveCommand.ExecuteAsync(null);

        saved.ShouldNotBeNull().Username.ShouldBe("ada@example.com");
        editor.ErrorMessage.ShouldBeNull();
    }

    [Fact]
    public async Task SavingAnExistingUserWithoutAPasswordKeepsTheOldPassword()
    {
        var user = await AddAsync();
        var editor = Editor(user);
        editor.FirstName = "Augusta";
        UserDto? saved = null;
        editor.Saved += (_, dto) => saved = dto;

        await editor.SaveCommand.ExecuteAsync(null);

        saved.ShouldNotBeNull().Id.ShouldBe(user.Id);
        saved.FirstName.ShouldBe("Augusta");
        editor.ErrorMessage.ShouldBeNull();
    }

    [Fact]
    public async Task AMissingDateOfBirthIsRejectedBeforeAnythingIsSent()
    {
        var editor = Editor();
        Fill(editor);
        editor.DateOfBirth = null;
        var raised = false;
        editor.Saved += (_, _) => raised = true;

        await editor.SaveCommand.ExecuteAsync(null);

        editor.ErrorMessage.ShouldBe(Error("date_of_birth_required"));
        raised.ShouldBeFalse();
    }

    [Fact]
    public async Task ADateOfBirthInTheFutureIsRejected()
    {
        var editor = Editor();
        Fill(editor);
        editor.DateOfBirth = new DateTime(2099, 1, 1);

        await editor.SaveCommand.ExecuteAsync(null);

        editor.ErrorMessage.ShouldBe(Error("date_of_birth_future"));
    }

    [Fact]
    public async Task ABadUsernameAndATooShortPasswordAreRejected()
    {
        var editor = Editor();
        Fill(editor, "not an email");
        await editor.SaveCommand.ExecuteAsync(null);
        editor.ErrorMessage.ShouldBe(Error("username_invalid"));

        editor.Username = "ada@example.com";
        editor.Password = "short";
        await editor.SaveCommand.ExecuteAsync(null);
        editor.ErrorMessage.ShouldBe(Error("password_too_short"));

        editor.Password = string.Empty;
        await editor.SaveCommand.ExecuteAsync(null);
        editor.ErrorMessage.ShouldBe(Error("password_required"));
    }

    [Fact]
    public async Task ADuplicateUsernameIsRejectedAndTheErrorClearsOnTheNextGoodSave()
    {
        await AddAsync();
        var editor = Editor();
        Fill(editor);

        await editor.SaveCommand.ExecuteAsync(null);
        editor.ErrorMessage.ShouldBe(Error("user_conflict"));

        editor.Username = "bo@example.com";
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
        await AddAsync("ada@example.com");
        await AddAsync("bo@example.com");
        var list = List();

        await list.LoadCommand.ExecuteAsync(null);
        list.Users.Count.ShouldBe(2);
        list.StatusMessage.ShouldBe(_localizer.Format("Users_Count", 2));

        list.Search = "bo@";
        await list.LoadCommand.ExecuteAsync(null);
        list.Users.Single().Username.ShouldBe("bo@example.com");
    }

    [Fact]
    public async Task ReloadingKeepsTheSelectionWithoutReopeningTheEditor()
    {
        await AddAsync();
        var list = List();
        await list.LoadCommand.ExecuteAsync(null);
        list.SelectedUser = list.Users.Single();
        var editor = list.Editor;

        await list.LoadCommand.ExecuteAsync(null);

        list.SelectedUser.ShouldNotBeNull();
        list.Editor.ShouldBeSameAs(editor);
    }

    [Fact]
    public async Task SelectingAndNewOpenTheEditorAndCancelClosesIt()
    {
        await AddAsync();
        var list = List();
        await list.LoadCommand.ExecuteAsync(null);

        list.SelectedUser = list.Users.Single();
        list.Editor.ShouldNotBeNull().Username.ShouldBe("ada@example.com");

        list.NewCommand.Execute(null);
        list.SelectedUser.ShouldBeNull();
        list.Editor.ShouldNotBeNull().IsNew.ShouldBeTrue();

        list.Editor.CancelCommand.Execute(null);
        list.Editor.ShouldBeNull();
    }

    [Fact]
    public async Task SavingFromTheEditorClosesItAndReloads()
    {
        var list = List();
        list.NewCommand.Execute(null);
        Fill(list.Editor!);

        await list.Editor!.SaveCommand.ExecuteAsync(null);

        await WaitUntilAsync(() => list.StatusMessage == _localizer.Format("Users_Saved", "Ada Lovelace"));
        list.Editor.ShouldBeNull();
        list.Users.Count.ShouldBe(1);
    }

    [Fact]
    public async Task DetailOpensForTheSelectionAndBackReturnsToTheList()
    {
        await AddAsync();
        var list = List();
        list.OpenDetailCommand.CanExecute(null).ShouldBeFalse();
        await list.LoadCommand.ExecuteAsync(null);
        list.SelectedUser = list.Users.Single();

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
        list.SelectedUser = list.Users.Single();

        list.RequestDeleteCommand.Execute(null);
        list.IsConfirmingDelete.ShouldBeTrue();
        list.CancelDeleteCommand.Execute(null);

        list.IsConfirmingDelete.ShouldBeFalse();
        list.Users.Count.ShouldBe(1);
    }

    [Fact]
    public async Task ConfirmingTheDeleteRemovesTheUserAndTellsTheOtherModules()
    {
        await AddAsync();
        var list = List();
        await list.LoadCommand.ExecuteAsync(null);
        list.SelectedUser = list.Users.Single();
        list.RequestDeleteCommand.Execute(null);

        await list.ConfirmDeleteCommand.ExecuteAsync(null);

        list.Users.ShouldBeEmpty();
        list.Editor.ShouldBeNull();
        _fixture.Events.Published.Count.ShouldBe(1);
    }

    [Fact]
    public async Task DeletingAUserThatIsAlreadyGoneShowsTheError()
    {
        var user = await AddAsync();
        var list = List();
        await list.LoadCommand.ExecuteAsync(null);
        list.SelectedUser = list.Users.Single();
        await _fixture.Delete.HandleAsync(new DeleteUser.Command(user.Id), Ct);

        await list.ConfirmDeleteCommand.ExecuteAsync(null);

        list.StatusMessage.ShouldBe(Error("user_not_found"));
    }
}
