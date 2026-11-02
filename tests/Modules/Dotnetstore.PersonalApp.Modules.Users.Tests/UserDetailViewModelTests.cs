using Dotnetstore.PersonalApp.Modules.Users.Features.Users;

namespace Dotnetstore.PersonalApp.Modules.Users.Tests;

public sealed class UserDetailViewModelTests
{
    private static UserDto Ada() =>
        new(Guid.NewGuid(), "Ada", "Lovelace", "ada@example.com", new DateOnly(1815, 12, 10), DateTimeOffset.UnixEpoch);

    [Fact]
    public void InitialsComeFromTheFirstAndLastName()
    {
        var viewModel = new UserDetailViewModel(Ada());

        viewModel.Initials.ShouldBe("AL");
    }

    [Fact]
    public void BackAndEditRaiseTheirEvents()
    {
        var viewModel = new UserDetailViewModel(Ada());
        var closed = 0;
        var editRequested = 0;
        viewModel.Closed += (_, _) => closed++;
        viewModel.EditRequested += (_, _) => editRequested++;

        viewModel.BackCommand.Execute(null);
        viewModel.EditCommand.Execute(null);

        closed.ShouldBe(1);
        editRequested.ShouldBe(1);
    }
}
