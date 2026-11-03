using Dotnetstore.PersonalApp.Modules.Grades.Features.Grades;
using Dotnetstore.PersonalApp.Shared.Auth;
using Dotnetstore.PersonalApp.Shared.Localization;
using Dotnetstore.PersonalApp.Shared.Mvvm;
using Dotnetstore.PersonalApp.Shared.Shell;

namespace Dotnetstore.PersonalApp.Modules.Grades;

/// <summary>The signed-in teacher's grades for all students.</summary>
internal sealed class GradesPage(GradesViewModelFactory factory, ICurrentUser currentUser) : IModulePage
{
    public string? ModuleKey => SchoolGroup.ModuleKey;

    public ModulePageGroup? Group => SchoolGroup.Create();

    public string Title => Localizer.Instance.Get("Grades_Title");

    // Material "grade" (star) icon
    public string IconData =>
        "M12 17.27L18.18 21l-1.64-7.03L22 9.24l-7.19-.61L12 2 9.19 8.63 2 9.24l5.46 4.73L5.82 21z";

    public ViewModelBase CreateViewModel() => factory.CreateForPage(currentUser.User?.Id ?? Guid.Empty);
}
