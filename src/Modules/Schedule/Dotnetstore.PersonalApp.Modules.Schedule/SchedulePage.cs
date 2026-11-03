using Dotnetstore.PersonalApp.Modules.Schedule.Features.Entries;
using Dotnetstore.PersonalApp.Shared.Auth;
using Dotnetstore.PersonalApp.Shared.Clients;
using Dotnetstore.PersonalApp.Shared.Localization;
using Dotnetstore.PersonalApp.Shared.Mvvm;
using Dotnetstore.PersonalApp.Shared.Shell;
using Dotnetstore.PersonalApp.Shared.Students;
using Dotnetstore.PersonalApp.Shared.Subjects;

namespace Dotnetstore.PersonalApp.Modules.Schedule;

/// <summary>The signed-in teacher's recurring weekly lessons.</summary>
internal sealed class SchedulePage(
    ICurrentUser currentUser,
    ListScheduleEntries.Handler list,
    CreateScheduleEntry.Handler create,
    UpdateScheduleEntry.Handler update,
    DeleteScheduleEntry.Handler delete,
    ISubjectLookup subjects,
    IClientLookup clients,
    IStudentLookup students,
    ILocalizationService localization) : IModulePage
{
    public string? ModuleKey => SchoolGroup.ModuleKey;

    public ModulePageGroup? Group => SchoolGroup.Create();

    public string Title => Localizer.Instance.Get("Schedule_Title");

    // Material "calendar_month" icon
    public string IconData =>
        "M19 4h-1V2h-2v2H8V2H6v2H5c-1.11 0-1.99.9-1.99 2L3 20c0 1.1.89 2 2 2h14c1.1 0 2-.9 2-2V6c0-1.1-.9-2-2-2zm0 16H5V10h14v10zm0-12H5V6h14v2zM7 12h5v5H7z";

    public ViewModelBase CreateViewModel() =>
        new ScheduleViewModel(currentUser.User?.Id ?? Guid.Empty, list, create, update, delete, subjects, clients, students, localization);
}
