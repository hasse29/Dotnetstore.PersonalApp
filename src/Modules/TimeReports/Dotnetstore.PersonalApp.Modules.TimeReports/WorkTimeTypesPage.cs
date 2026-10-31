using Dotnetstore.PersonalApp.Modules.TimeReports.Features.WorkTimeTypes;
using Dotnetstore.PersonalApp.Shared.Localization;
using Dotnetstore.PersonalApp.Shared.Mvvm;
using Dotnetstore.PersonalApp.Shared.Shell;
using Microsoft.Extensions.DependencyInjection;

namespace Dotnetstore.PersonalApp.Modules.TimeReports;

internal sealed class WorkTimeTypesPage(IServiceProvider services) : IModulePage
{
    public string? ModuleKey => "timereports";

    public string Title => Localizer.Instance.Get("WorkTimeTypes_Title");

    // Material "work" icon
    public string IconData =>
        "M20 6h-4V4c0-1.11-.89-2-2-2h-4c-1.11 0-2 .89-2 2v2H4c-1.11 0-1.99.89-1.99 2L2 19c0 1.11.89 2 2 2h16c1.11 0 2-.89 2-2V8c0-1.11-.89-2-2-2zm-6 0h-4V4h4v2z";

    public ModulePageGroup? Group => TimeReportsNav.Group();

    public ViewModelBase CreateViewModel() => services.GetRequiredService<WorkTimeTypesViewModel>();
}