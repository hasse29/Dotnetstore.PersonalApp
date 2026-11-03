using Dotnetstore.PersonalApp.Modules.Grades.Features.Scales;
using Dotnetstore.PersonalApp.Shared.Localization;
using Dotnetstore.PersonalApp.Shared.Mvvm;
using Dotnetstore.PersonalApp.Shared.Shell;
using Microsoft.Extensions.DependencyInjection;

namespace Dotnetstore.PersonalApp.Modules.Grades;

/// <summary>The grading systems in use, e.g. Sweden's A-F or Germany's 1-6, with their grades.</summary>
internal sealed class GradeScalesPage(IServiceProvider services) : IModulePage
{
    public string? ModuleKey => SchoolGroup.ModuleKey;

    public ModulePageGroup? Group => SchoolGroup.Create();

    public string Title => Localizer.Instance.Get("GradeScales_Title");

    // Material "format_list_numbered" icon
    public string IconData =>
        "M2 17h2v.5H3v1h1v.5H2v1h3v-4H2v1zm1-9h1V4H2v1h1v3zm-1 3h1.8L2 13.1v.9h3v-1H3.2L5 10.9V10H2v1zm5-6v2h14V5H7zm0 14h14v-2H7v2zm0-6h14v-2H7v2z";

    public ViewModelBase CreateViewModel() => services.GetRequiredService<GradeScalesViewModel>();
}
