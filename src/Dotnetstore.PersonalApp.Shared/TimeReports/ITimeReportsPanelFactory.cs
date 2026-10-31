using Dotnetstore.PersonalApp.Shared.Mvvm;

namespace Dotnetstore.PersonalApp.Shared.TimeReports;

/// <summary>
/// Lets the Clients module show the signed-in user's time reports for one client on its detail page without referencing the
/// TimeReports module. The returned view model is resolved to a view by the host's <c>ViewLocator</c>.
/// </summary>
public interface ITimeReportsPanelFactory
{
    ViewModelBase CreateForClient(Guid clientId);
}