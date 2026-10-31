using Dotnetstore.PersonalApp.Shared.Localization;
using Dotnetstore.PersonalApp.Shared.Mvvm;
using Dotnetstore.PersonalApp.Shared.Shell;

namespace Dotnetstore.PersonalApp.Modules.TimeReports;

internal sealed class TimeReportsPage(TimeReportsPanelFactory factory) : IModulePage
{
    public string? ModuleKey => "timereports";

    public string Title => Localizer.Instance.Get("TimeReports_Reports");

    // Material "schedule" icon
    public string IconData =>
        "M11.99 2C6.47 2 2 6.48 2 12s4.47 10 9.99 10C17.52 22 22 17.52 22 12S17.52 2 11.99 2zM12 20c-4.42 0-8-3.58-8-8s3.58-8 8-8 8 3.58 8 8-3.58 8-8 8zm.5-13H11v6l5.25 3.15.75-1.23-4.5-2.67z";

    public ModulePageGroup? Group => TimeReportsNav.Group();

    public ViewModelBase CreateViewModel() => factory.CreateForAllClients();
}