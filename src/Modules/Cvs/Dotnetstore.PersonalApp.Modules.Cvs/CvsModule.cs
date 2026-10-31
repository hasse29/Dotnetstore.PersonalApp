using Dotnetstore.PersonalApp.Modules.Cvs.Consumers;
using Dotnetstore.PersonalApp.Modules.Cvs.Features.Cvs;
using Dotnetstore.PersonalApp.Modules.Cvs.Persistence;
using Dotnetstore.PersonalApp.Modules.Cvs.Resources;
using Dotnetstore.PersonalApp.Modules.Cvs.Startup;
using Dotnetstore.PersonalApp.Shared.Contracts.Events;
using Dotnetstore.PersonalApp.Shared.Cvs;
using Dotnetstore.PersonalApp.Shared.Events;
using Dotnetstore.PersonalApp.Shared.Localization;
using Dotnetstore.PersonalApp.Shared.Shell;
using Dotnetstore.PersonalApp.Shared.Startup;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Dotnetstore.PersonalApp.Modules.Cvs;

public static class CvsModule
{
    public static IServiceCollection AddCvsModule(this IServiceCollection services, string databasePath)
    {
        Localizer.Instance.AddResources(CvsStrings.ResourceManager);

        services.AddDbContextFactory<CvsDbContext>(options => options.UseSqlite($"Data Source={databasePath}"));

        services.AddSingleton<IValidator<CreateCv.Command>, CreateCv.Validator>();
        services.AddSingleton<IValidator<UpdateCv.Command>, UpdateCv.Validator>();
        services.AddSingleton<IValidator<SaveWorkExperience.Command>, SaveWorkExperience.Validator>();
        services.AddSingleton<IValidator<SaveEducation.Command>, SaveEducation.Validator>();
        services.AddSingleton<IValidator<SaveCourse.Command>, SaveCourse.Validator>();
        services.AddSingleton<IValidator<SaveLanguageSkill.Command>, SaveLanguageSkill.Validator>();

        services.AddSingleton<CreateCv.Handler>();
        services.AddSingleton<GetCv.Handler>();
        services.AddSingleton<ListCvs.Handler>();
        services.AddSingleton<UpdateCv.Handler>();
        services.AddSingleton<DeleteCv.Handler>();
        services.AddSingleton<DeleteCvsForUser.Handler>();

        services.AddSingleton<GetCvDetail.Handler>();
        services.AddSingleton<SaveWorkExperience.Handler>();
        services.AddSingleton<SaveEducation.Handler>();
        services.AddSingleton<SaveCourse.Handler>();
        services.AddSingleton<SaveLanguageSkill.Handler>();
        services.AddSingleton<DeleteCvSectionItem.Handler>();
        services.AddSingleton<SetDriverLicence.Handler>();
        services.AddSingleton<SetCvContacts.Handler>();
        services.AddSingleton<SetCvPhoto.Handler>();
        services.AddSingleton<ExportCvToWord.Handler>();
        services.AddSingleton<CvDetailServices>();

        services.AddSingleton<CvsPanelFactory>();
        services.AddSingleton<IUserCvsPanelFactory>(sp => sp.GetRequiredService<CvsPanelFactory>());

        services.AddSingleton<IIntegrationEventConsumer<UserDeleted>, UserDeletedConsumer>();

        services.AddSingleton<IModulePage, CvsPage>();
        services.AddSingleton(new ModuleInfo("cvs", "Cvs_Title"));
        services.AddSingleton<IStartupTask, MigrateCvsDatabaseTask>();

        return services;
    }
}
