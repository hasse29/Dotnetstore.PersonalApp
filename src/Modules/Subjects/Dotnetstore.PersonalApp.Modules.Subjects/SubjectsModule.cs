using Dotnetstore.PersonalApp.Modules.Subjects.Consumers;
using Dotnetstore.PersonalApp.Modules.Subjects.Features.Subjects;
using Dotnetstore.PersonalApp.Modules.Subjects.Features.TeacherSubjects;
using Dotnetstore.PersonalApp.Modules.Subjects.Persistence;
using Dotnetstore.PersonalApp.Modules.Subjects.Resources;
using Dotnetstore.PersonalApp.Modules.Subjects.Startup;
using Dotnetstore.PersonalApp.Shared.Contracts.Events;
using Dotnetstore.PersonalApp.Shared.Events;
using Dotnetstore.PersonalApp.Shared.Localization;
using Dotnetstore.PersonalApp.Shared.Shell;
using Dotnetstore.PersonalApp.Shared.Startup;
using Dotnetstore.PersonalApp.Shared.Subjects;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Dotnetstore.PersonalApp.Modules.Subjects;

public static class SubjectsModule
{
    public static IServiceCollection AddSubjectsModule(this IServiceCollection services, string databasePath)
    {
        Localizer.Instance.AddResources(SubjectsStrings.ResourceManager);

        services.AddDbContextFactory<SubjectsDbContext>(options => options.UseSqlite($"Data Source={databasePath}"));

        services.AddSingleton<IValidator<CreateSubject.Command>, CreateSubject.Validator>();
        services.AddSingleton<IValidator<UpdateSubject.Command>, UpdateSubject.Validator>();

        services.AddSingleton<CreateSubject.Handler>();
        services.AddSingleton<GetSubject.Handler>();
        services.AddSingleton<ListSubjects.Handler>();
        services.AddSingleton<UpdateSubject.Handler>();
        services.AddSingleton<DeleteSubject.Handler>();

        services.AddSingleton<ListTeacherSubjects.Handler>();
        services.AddSingleton<AddTeacherSubject.Handler>();
        services.AddSingleton<RemoveTeacherSubject.Handler>();
        services.AddSingleton<DeleteTeacherSubjectsForUser.Handler>();
        services.AddSingleton<IUserSubjectsPanelFactory, UserSubjectsPanelFactory>();

        services.AddSingleton<IIntegrationEventConsumer<UserDeleted>, UserDeletedConsumer>();

        services.AddTransient<SubjectsViewModel>();
        services.AddSingleton<IModulePage, SubjectsPage>();
        services.AddSingleton(new ModuleInfo("subjects", "Subjects_Title"));
        services.AddSingleton<IStartupTask, MigrateSubjectsDatabaseTask>();

        return services;
    }
}
