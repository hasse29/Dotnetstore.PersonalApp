using Dotnetstore.PersonalApp.Modules.TimeReports.Features.TimeReports;
using Dotnetstore.PersonalApp.Modules.TimeReports.Features.WorkTimeTypes;
using Dotnetstore.PersonalApp.Modules.TimeReports.Persistence;
using Dotnetstore.PersonalApp.Modules.TimeReports.Resources;
using Dotnetstore.PersonalApp.Modules.TimeReports.Startup;
using Dotnetstore.PersonalApp.Shared.Localization;
using Dotnetstore.PersonalApp.Shared.Shell;
using Dotnetstore.PersonalApp.Shared.Startup;
using Dotnetstore.PersonalApp.Shared.TimeReports;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Dotnetstore.PersonalApp.Modules.TimeReports;

public static class TimeReportsModule
{
    public static IServiceCollection AddTimeReportsModule(this IServiceCollection services, string databasePath)
    {
        Localizer.Instance.AddResources(TimeReportsStrings.ResourceManager);

        services.AddDbContextFactory<TimeReportsDbContext>(options => options.UseSqlite($"Data Source={databasePath}"));

        services.AddSingleton<IValidator<CreateTimeReport.Command>, CreateTimeReport.Validator>();
        services.AddSingleton<IValidator<UpdateTimeReport.Command>, UpdateTimeReport.Validator>();

        services.AddSingleton<CreateTimeReport.Handler>();
        services.AddSingleton<GetTimeReport.Handler>();
        services.AddSingleton<ListTimeReports.Handler>();
        services.AddSingleton<UpdateTimeReport.Handler>();
        services.AddSingleton<DeleteTimeReport.Handler>();
        services.AddSingleton<ExportTimeReports.Handler>();
        services.AddSingleton<ExportKilometerSummary.Handler>();

        services.AddSingleton<IValidator<CreateWorkTimeType.Command>, CreateWorkTimeType.Validator>();
        services.AddSingleton<IValidator<UpdateWorkTimeType.Command>, UpdateWorkTimeType.Validator>();

        services.AddSingleton<CreateWorkTimeType.Handler>();
        services.AddSingleton<GetWorkTimeType.Handler>();
        services.AddSingleton<ListWorkTimeTypes.Handler>();
        services.AddSingleton<UpdateWorkTimeType.Handler>();
        services.AddSingleton<DeleteWorkTimeType.Handler>();

        services.AddTransient<WorkTimeTypesViewModel>();
        services.AddSingleton<IModulePage, WorkTimeTypesPage>();

        services.AddSingleton<TimeReportsPanelFactory>();
        services.AddSingleton<ITimeReportsPanelFactory>(sp => sp.GetRequiredService<TimeReportsPanelFactory>());

        services.AddSingleton<IModulePage, TimeReportsPage>();
        services.AddSingleton(new ModuleInfo("timereports", "TimeReports_Title"));
        services.AddSingleton<IStartupTask, MigrateTimeReportsDatabaseTask>();

        return services;
    }
}