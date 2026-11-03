using Dotnetstore.PersonalApp.Modules.Grades.Features.Tests;
using Dotnetstore.PersonalApp.Shared.Auth;
using Dotnetstore.PersonalApp.Shared.Localization;
using Dotnetstore.PersonalApp.Shared.Mvvm;
using Dotnetstore.PersonalApp.Shared.Shell;

namespace Dotnetstore.PersonalApp.Modules.Grades;

/// <summary>The signed-in teacher's tests, with each student's score and grade.</summary>
internal sealed class TestsPage(TestsViewModelFactory factory, ICurrentUser currentUser) : IModulePage
{
    public string? ModuleKey => "grades";

    public ModulePageGroup? Group => SchoolGroup.Create();

    public string Title => Localizer.Instance.Get("Tests_Title");

    // Material "assignment" icon
    public string IconData =>
        "M19 3h-4.18C14.4 1.84 13.3 1 12 1s-2.4.84-2.82 2H5c-1.1 0-2 .9-2 2v14c0 1.1.9 2 2 2h14c1.1 0 2-.9 2-2V5c0-1.1-.9-2-2-2zm-7 0c.55 0 1 .45 1 1s-.45 1-1 1-1-.45-1-1 .45-1 1-1zm2 14H7v-2h7v2zm3-4H7v-2h10v2zm0-4H7V7h10v2z";

    public ViewModelBase CreateViewModel() => factory.Create(currentUser.User?.Id ?? Guid.Empty);
}
