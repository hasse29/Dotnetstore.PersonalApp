using Dotnetstore.PersonalApp.Modules.Schedule.Dashboard;
using Dotnetstore.PersonalApp.Shared.Dashboard;
using Dotnetstore.PersonalApp.Modules.Schedule.Consumers;
using Dotnetstore.PersonalApp.Modules.Schedule.Features.Entries;
using Dotnetstore.PersonalApp.Modules.Schedule.Persistence;
using Dotnetstore.PersonalApp.Modules.Schedule.Resources;
using Dotnetstore.PersonalApp.Modules.Schedule.Startup;
using Dotnetstore.PersonalApp.Shared.Contracts.Events;
using Dotnetstore.PersonalApp.Shared.Events;
using Dotnetstore.PersonalApp.Shared.Localization;
using Dotnetstore.PersonalApp.Shared.Shell;
using Dotnetstore.PersonalApp.Shared.Startup;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Dotnetstore.PersonalApp.Modules.Schedule;

public static class ScheduleModule
{
    public static IServiceCollection AddScheduleModule(this IServiceCollection services, string databasePath)
    {
        Localizer.Instance.AddResources(ScheduleStrings.ResourceManager);

        services.AddDbContextFactory<ScheduleDbContext>(options => options.UseSqlite($"Data Source={databasePath}"));

        services.AddSingleton<IValidator<CreateScheduleEntry.Command>, CreateScheduleEntry.Validator>();
        services.AddSingleton<IValidator<UpdateScheduleEntry.Command>, UpdateScheduleEntry.Validator>();

        services.AddSingleton<CreateScheduleEntry.Handler>();
        services.AddSingleton<UpdateScheduleEntry.Handler>();
        services.AddSingleton<DeleteScheduleEntry.Handler>();
        services.AddSingleton<ListScheduleEntries.Handler>();
        services.AddSingleton<DeleteScheduleEntriesFor.Handler>();
        services.AddSingleton<RemoveStudentFromEntries.Handler>();

        services.AddSingleton<IIntegrationEventConsumer<UserDeleted>, UserDeletedConsumer>();
        services.AddSingleton<IIntegrationEventConsumer<ClientDeleted>, ClientDeletedConsumer>();
        services.AddSingleton<IIntegrationEventConsumer<SubjectDeleted>, SubjectDeletedConsumer>();
        services.AddSingleton<IIntegrationEventConsumer<StudentDeleted>, StudentDeletedConsumer>();

        services.AddSingleton<IModulePage, SchedulePage>();
        services.AddSingleton<IDashboardWidget, ScheduleDashboardWidget>();
        services.AddSingleton<IStartupTask, MigrateScheduleDatabaseTask>();

        return services;
    }
}
