using Dotnetstore.PersonalApp.Modules.Students.Features.Students;
using Dotnetstore.PersonalApp.Shared.Localization;
using Dotnetstore.PersonalApp.Shared.Mvvm;
using Dotnetstore.PersonalApp.Shared.Shell;

namespace Dotnetstore.PersonalApp.Modules.Students;

internal sealed class StudentsPage(StudentsViewModelFactory factory) : IModulePage
{
    public string? ModuleKey => "students";

    public ModulePageGroup? Group => SchoolGroup.Create();

    public string Title => Localizer.Instance.Get("Students_Title");

    // Material "school" icon
    public string IconData =>
        "M5 13.18v4L12 21l7-3.82v-4L12 17l-7-3.82zM12 3L1 9l11 6 9-4.91V17h2V9L12 3z";

    public ViewModelBase CreateViewModel() => factory.CreateForPage();
}
