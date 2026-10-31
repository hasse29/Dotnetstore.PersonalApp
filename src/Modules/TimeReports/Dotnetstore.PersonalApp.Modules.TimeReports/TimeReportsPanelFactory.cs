using Dotnetstore.PersonalApp.Modules.TimeReports.Features.TimeReports;
using Dotnetstore.PersonalApp.Modules.TimeReports.Features.WorkTimeTypes;
using Dotnetstore.PersonalApp.Shared.Auth;
using Dotnetstore.PersonalApp.Shared.Clients;
using Dotnetstore.PersonalApp.Shared.Files;
using Dotnetstore.PersonalApp.Shared.Localization;
using Dotnetstore.PersonalApp.Shared.Mvvm;
using Dotnetstore.PersonalApp.Shared.TimeReports;

namespace Dotnetstore.PersonalApp.Modules.TimeReports;

internal sealed class TimeReportsPanelFactory(
    ICurrentUser currentUser,
    ListTimeReports.Handler list,
    CreateTimeReport.Handler create,
    UpdateTimeReport.Handler update,
    DeleteTimeReport.Handler delete,
    ExportTimeReports.Handler export,
    ExportKilometerSummary.Handler exportKilometers,
    IFileSaveService fileSaver,
    ListWorkTimeTypes.Handler workTimeTypes,
    IClientLookup clients,
    ILocalizationService localization,
    TimeProvider clock) : ITimeReportsPanelFactory
{
    /// <summary>The all-clients view used by the module's own page.</summary>
    public TimeReportsViewModel CreateForAllClients() => Build(null);

    public ViewModelBase CreateForClient(Guid clientId) => Build(clientId);

    private TimeReportsViewModel Build(Guid? clientId) =>
        new(clientId, currentUser, list, create, update, delete, export, exportKilometers, fileSaver, workTimeTypes, clients, localization, clock);
}