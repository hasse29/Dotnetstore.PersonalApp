using Dotnetstore.PersonalApp.Modules.Grades.Consumers;
using Dotnetstore.PersonalApp.Modules.Grades.Features.Grades;
using Dotnetstore.PersonalApp.Modules.Grades.Features.Scales;
using Dotnetstore.PersonalApp.Modules.Grades.Persistence;
using Dotnetstore.PersonalApp.Modules.Grades.Resources;
using Dotnetstore.PersonalApp.Modules.Grades.Startup;
using Dotnetstore.PersonalApp.Shared.Contracts.Events;
using Dotnetstore.PersonalApp.Shared.Events;
using Dotnetstore.PersonalApp.Shared.Localization;
using Dotnetstore.PersonalApp.Shared.Shell;
using Dotnetstore.PersonalApp.Shared.Startup;
using Dotnetstore.PersonalApp.Shared.Students;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Dotnetstore.PersonalApp.Modules.Grades;

public static class GradesModule
{
    public static IServiceCollection AddGradesModule(this IServiceCollection services, string databasePath)
    {
        Localizer.Instance.AddResources(GradesStrings.ResourceManager);

        services.AddDbContextFactory<GradesDbContext>(options => options.UseSqlite($"Data Source={databasePath}"));

        services.AddSingleton<IValidator<CreateGradeScale.Command>, CreateGradeScale.Validator>();
        services.AddSingleton<IValidator<UpdateGradeScale.Command>, UpdateGradeScale.Validator>();
        services.AddSingleton<IValidator<CreateGrade.Command>, CreateGrade.Validator>();
        services.AddSingleton<IValidator<UpdateGrade.Command>, UpdateGrade.Validator>();

        services.AddSingleton<ListGradeScales.Handler>();
        services.AddSingleton<CreateGradeScale.Handler>();
        services.AddSingleton<UpdateGradeScale.Handler>();
        services.AddSingleton<DeleteGradeScale.Handler>();
        services.AddSingleton<ListGrades.Handler>();
        services.AddSingleton<CreateGrade.Handler>();
        services.AddSingleton<UpdateGrade.Handler>();
        services.AddSingleton<DeleteGrade.Handler>();
        services.AddSingleton<DeleteGradesFor.Handler>();

        services.AddSingleton<GradeScalesViewModel>();
        services.AddSingleton<GradesViewModelFactory>();
        services.AddSingleton<IStudentGradesPanelFactory, StudentGradesPanelFactory>();

        services.AddSingleton<IIntegrationEventConsumer<UserDeleted>, UserDeletedConsumer>();
        services.AddSingleton<IIntegrationEventConsumer<StudentDeleted>, StudentDeletedConsumer>();
        services.AddSingleton<IIntegrationEventConsumer<SubjectDeleted>, SubjectDeletedConsumer>();

        services.AddSingleton<IModulePage, GradesPage>();
        services.AddSingleton<IModulePage, GradeScalesPage>();
        services.AddSingleton(new ModuleInfo("grades", "Grades_Title"));
        services.AddSingleton<IStartupTask, MigrateGradesDatabaseTask>();

        return services;
    }
}
