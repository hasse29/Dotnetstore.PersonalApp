using Dotnetstore.PersonalApp.Modules.EmailAddresses.Consumers;
using Dotnetstore.PersonalApp.Modules.EmailAddresses.Features.EmailAddresses;
using Dotnetstore.PersonalApp.Modules.EmailAddresses.Features.EmailAddressTypes;
using Dotnetstore.PersonalApp.Modules.EmailAddresses.Persistence;
using Dotnetstore.PersonalApp.Modules.EmailAddresses.Resources;
using Dotnetstore.PersonalApp.Modules.EmailAddresses.Startup;
using Dotnetstore.PersonalApp.Shared.Contacts;
using Dotnetstore.PersonalApp.Shared.EmailAddresses;
using Dotnetstore.PersonalApp.Shared.Contracts.Events;
using Dotnetstore.PersonalApp.Shared.Events;
using Dotnetstore.PersonalApp.Shared.Localization;
using Dotnetstore.PersonalApp.Shared.Shell;
using Dotnetstore.PersonalApp.Shared.Startup;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Dotnetstore.PersonalApp.Modules.EmailAddresses;

public static class EmailAddressesModule
{
    public static IServiceCollection AddEmailAddressesModule(this IServiceCollection services, string databasePath)
    {
        Localizer.Instance.AddResources(EmailAddressesStrings.ResourceManager);

        services.AddDbContextFactory<EmailAddressesDbContext>(options => options.UseSqlite($"Data Source={databasePath}"));

        services.AddSingleton<IValidator<CreateEmailAddressType.Command>, CreateEmailAddressType.Validator>();
        services.AddSingleton<IValidator<UpdateEmailAddressType.Command>, UpdateEmailAddressType.Validator>();

        services.AddSingleton<CreateEmailAddressType.Handler>();
        services.AddSingleton<GetEmailAddressType.Handler>();
        services.AddSingleton<ListEmailAddressTypes.Handler>();
        services.AddSingleton<UpdateEmailAddressType.Handler>();
        services.AddSingleton<DeleteEmailAddressType.Handler>();

        services.AddSingleton<IValidator<CreateEmailAddress.Command>, CreateEmailAddress.Validator>();
        services.AddSingleton<IValidator<UpdateEmailAddress.Command>, UpdateEmailAddress.Validator>();

        services.AddSingleton<CreateEmailAddress.Handler>();
        services.AddSingleton<GetEmailAddress.Handler>();
        services.AddSingleton<ListEmailAddresses.Handler>();
        services.AddSingleton<UpdateEmailAddress.Handler>();
        services.AddSingleton<DeleteEmailAddress.Handler>();

        services.AddSingleton<DeleteEmailAddressesForObject.Handler>();
        services.AddSingleton<IObjectEmailAddressesPanelFactory, ObjectEmailAddressesPanelFactory>();
        services.AddSingleton<IContactInfoSource, EmailContactSource>();

        services.AddSingleton<IIntegrationEventConsumer<UserDeleted>, UserDeletedConsumer>();
        services.AddSingleton<IIntegrationEventConsumer<ClientDeleted>, ClientDeletedConsumer>();
        services.AddSingleton<IIntegrationEventConsumer<ContactPersonDeleted>, ContactPersonDeletedConsumer>();

        services.AddTransient<EmailAddressTypesViewModel>();
        services.AddSingleton<IModulePage, EmailAddressesPage>();
        services.AddSingleton(new ModuleInfo("emailaddresses", "EmailAddressesPanel_Title"));
        services.AddSingleton<IStartupTask, MigrateEmailAddressesDatabaseTask>();

        return services;
    }
}
