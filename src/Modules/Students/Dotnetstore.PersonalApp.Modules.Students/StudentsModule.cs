using Dotnetstore.PersonalApp.Modules.Students.Consumers;
using Dotnetstore.PersonalApp.Modules.Students.Features.Notes;
using Dotnetstore.PersonalApp.Modules.Students.Features.Students;
using Dotnetstore.PersonalApp.Modules.Students.Persistence;
using Dotnetstore.PersonalApp.Modules.Students.Resources;
using Dotnetstore.PersonalApp.Modules.Students.Startup;
using Dotnetstore.PersonalApp.Shared.Contracts.Events;
using Dotnetstore.PersonalApp.Shared.Events;
using Dotnetstore.PersonalApp.Shared.Localization;
using Dotnetstore.PersonalApp.Shared.Shell;
using Dotnetstore.PersonalApp.Shared.Startup;
using Dotnetstore.PersonalApp.Shared.Students;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Dotnetstore.PersonalApp.Modules.Students;

public static class StudentsModule
{
    public static IServiceCollection AddStudentsModule(this IServiceCollection services, string databasePath)
    {
        Localizer.Instance.AddResources(StudentsStrings.ResourceManager);

        services.AddDbContextFactory<StudentsDbContext>(options => options.UseSqlite($"Data Source={databasePath}"));

        services.AddSingleton<IValidator<CreateStudent.Command>, CreateStudent.Validator>();
        services.AddSingleton<IValidator<UpdateStudent.Command>, UpdateStudent.Validator>();

        services.AddSingleton<IValidator<AddStudentNote.Command>, AddStudentNote.Validator>();
        services.AddSingleton<IValidator<UpdateStudentNote.Command>, UpdateStudentNote.Validator>();

        services.AddSingleton<ListStudents.Handler>();
        services.AddSingleton<CreateStudent.Handler>();
        services.AddSingleton<UpdateStudent.Handler>();
        services.AddSingleton<DeleteStudent.Handler>();
        services.AddSingleton<DeleteStudentsForClient.Handler>();
        services.AddSingleton<GetStudentPhoto.Handler>();
        services.AddSingleton<SetStudentPhoto.Handler>();
        services.AddSingleton<ListStudentNotes.Handler>();
        services.AddSingleton<AddStudentNote.Handler>();
        services.AddSingleton<UpdateStudentNote.Handler>();
        services.AddSingleton<DeleteStudentNote.Handler>();
        services.AddSingleton<StudentNotesViewModelFactory>();
        services.AddSingleton<StudentPhotoPanelFactory>();
        services.AddSingleton<StudentsViewModelFactory>();

        services.AddSingleton<IClientStudentsPanelFactory, StudentsPanelFactory>();
        services.AddSingleton<IStudentLookup, StudentLookup>();

        services.AddSingleton<IIntegrationEventConsumer<ClientDeleted>, ClientDeletedConsumer>();

        services.AddSingleton<IModulePage, StudentsPage>();
        services.AddSingleton(new ModuleInfo("students", "Students_Title"));
        services.AddSingleton<IStartupTask, MigrateStudentsDatabaseTask>();

        return services;
    }
}
