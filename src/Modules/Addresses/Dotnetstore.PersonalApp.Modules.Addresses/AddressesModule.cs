using Dotnetstore.PersonalApp.Modules.Addresses.Consumers;
using Dotnetstore.PersonalApp.Modules.Addresses.Features.Addresses;
using Dotnetstore.PersonalApp.Modules.Addresses.Features.AddressTypes;
using Dotnetstore.PersonalApp.Modules.Addresses.Persistence;
using Dotnetstore.PersonalApp.Modules.Addresses.Resources;
using Dotnetstore.PersonalApp.Modules.Addresses.Startup;
using Dotnetstore.PersonalApp.Shared.Addresses;
using Dotnetstore.PersonalApp.Shared.Contracts.Events;
using Dotnetstore.PersonalApp.Shared.Events;
using Dotnetstore.PersonalApp.Shared.Localization;
using Dotnetstore.PersonalApp.Shared.Shell;
using Dotnetstore.PersonalApp.Shared.Startup;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Dotnetstore.PersonalApp.Modules.Addresses;

public static class AddressesModule
{
    public static IServiceCollection AddAddressesModule(this IServiceCollection services, string databasePath)
    {
        Localizer.Instance.AddResources(AddressesStrings.ResourceManager);

        services.AddDbContextFactory<AddressesDbContext>(options => options.UseSqlite($"Data Source={databasePath}"));

        services.AddSingleton<IValidator<CreateAddressType.Command>, CreateAddressType.Validator>();
        services.AddSingleton<IValidator<UpdateAddressType.Command>, UpdateAddressType.Validator>();

        services.AddSingleton<CreateAddressType.Handler>();
        services.AddSingleton<GetAddressType.Handler>();
        services.AddSingleton<ListAddressTypes.Handler>();
        services.AddSingleton<UpdateAddressType.Handler>();
        services.AddSingleton<DeleteAddressType.Handler>();

        services.AddSingleton<IValidator<CreateAddress.Command>, CreateAddress.Validator>();
        services.AddSingleton<IValidator<UpdateAddress.Command>, UpdateAddress.Validator>();

        services.AddSingleton<CreateAddress.Handler>();
        services.AddSingleton<GetAddress.Handler>();
        services.AddSingleton<ListAddresses.Handler>();
        services.AddSingleton<UpdateAddress.Handler>();
        services.AddSingleton<DeleteAddress.Handler>();

        services.AddSingleton<DeleteAddressesForObject.Handler>();
        services.AddSingleton<IObjectAddressesPanelFactory, ObjectAddressesPanelFactory>();

        services.AddSingleton<IIntegrationEventConsumer<UserDeleted>, UserDeletedConsumer>();
        services.AddSingleton<IIntegrationEventConsumer<ClientDeleted>, ClientDeletedConsumer>();

        services.AddTransient<AddressTypesViewModel>();
        services.AddSingleton<IModulePage, AddressesPage>();
        services.AddSingleton(new ModuleInfo("addresses", "AddressesPanel_Title"));
        services.AddSingleton<IStartupTask, MigrateAddressesDatabaseTask>();

        return services;
    }
}
