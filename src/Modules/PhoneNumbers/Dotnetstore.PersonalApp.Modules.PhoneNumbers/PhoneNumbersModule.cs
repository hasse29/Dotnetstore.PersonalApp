using Dotnetstore.PersonalApp.Modules.PhoneNumbers.Consumers;
using Dotnetstore.PersonalApp.Modules.PhoneNumbers.Features.PhoneNumbers;
using Dotnetstore.PersonalApp.Modules.PhoneNumbers.Features.PhoneNumberTypes;
using Dotnetstore.PersonalApp.Modules.PhoneNumbers.Persistence;
using Dotnetstore.PersonalApp.Modules.PhoneNumbers.Resources;
using Dotnetstore.PersonalApp.Modules.PhoneNumbers.Startup;
using Dotnetstore.PersonalApp.Shared.PhoneNumbers;
using Dotnetstore.PersonalApp.Shared.Contracts.Events;
using Dotnetstore.PersonalApp.Shared.Events;
using Dotnetstore.PersonalApp.Shared.Localization;
using Dotnetstore.PersonalApp.Shared.Shell;
using Dotnetstore.PersonalApp.Shared.Startup;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Dotnetstore.PersonalApp.Modules.PhoneNumbers;

public static class PhoneNumbersModule
{
    public static IServiceCollection AddPhoneNumbersModule(this IServiceCollection services, string databasePath)
    {
        Localizer.Instance.AddResources(PhoneNumbersStrings.ResourceManager);

        services.AddDbContextFactory<PhoneNumbersDbContext>(options => options.UseSqlite($"Data Source={databasePath}"));

        services.AddSingleton<IValidator<CreatePhoneNumberType.Command>, CreatePhoneNumberType.Validator>();
        services.AddSingleton<IValidator<UpdatePhoneNumberType.Command>, UpdatePhoneNumberType.Validator>();

        services.AddSingleton<CreatePhoneNumberType.Handler>();
        services.AddSingleton<GetPhoneNumberType.Handler>();
        services.AddSingleton<ListPhoneNumberTypes.Handler>();
        services.AddSingleton<UpdatePhoneNumberType.Handler>();
        services.AddSingleton<DeletePhoneNumberType.Handler>();

        services.AddSingleton<IValidator<CreatePhoneNumber.Command>, CreatePhoneNumber.Validator>();
        services.AddSingleton<IValidator<UpdatePhoneNumber.Command>, UpdatePhoneNumber.Validator>();

        services.AddSingleton<CreatePhoneNumber.Handler>();
        services.AddSingleton<GetPhoneNumber.Handler>();
        services.AddSingleton<ListPhoneNumbers.Handler>();
        services.AddSingleton<UpdatePhoneNumber.Handler>();
        services.AddSingleton<DeletePhoneNumber.Handler>();

        services.AddSingleton<DeletePhoneNumbersForObject.Handler>();
        services.AddSingleton<IObjectPhoneNumbersPanelFactory, ObjectPhoneNumbersPanelFactory>();

        services.AddSingleton<IIntegrationEventConsumer<UserDeleted>, UserDeletedConsumer>();
        services.AddSingleton<IIntegrationEventConsumer<ClientDeleted>, ClientDeletedConsumer>();

        services.AddTransient<PhoneNumberTypesViewModel>();
        services.AddSingleton<IModulePage, PhoneNumbersPage>();
        services.AddSingleton(new ModuleInfo("phonenumbers", "PhoneNumbersPanel_Title"));
        services.AddSingleton<IStartupTask, MigratePhoneNumbersDatabaseTask>();

        return services;
    }
}
