using Dotnetstore.PersonalApp.Modules.Clients.Features.Clients;
using Dotnetstore.PersonalApp.Modules.Clients.Persistence;
using Dotnetstore.PersonalApp.Modules.Clients.Resources;
using Dotnetstore.PersonalApp.Modules.Clients.Startup;
using Dotnetstore.PersonalApp.Shared.Clients;
using Dotnetstore.PersonalApp.Shared.Localization;
using Dotnetstore.PersonalApp.Shared.Shell;
using Dotnetstore.PersonalApp.Shared.Startup;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Dotnetstore.PersonalApp.Modules.Clients;

public static class ClientsModule
{
    public static IServiceCollection AddClientsModule(this IServiceCollection services, string databasePath)
    {
        Localizer.Instance.AddResources(ClientsStrings.ResourceManager);

        services.AddDbContextFactory<ClientsDbContext>(options => options.UseSqlite($"Data Source={databasePath}"));

        services.AddSingleton<IValidator<CreateClient.Command>, CreateClient.Validator>();
        services.AddSingleton<IValidator<UpdateClient.Command>, UpdateClient.Validator>();

        services.AddSingleton<CreateClient.Handler>();
        services.AddSingleton<GetClient.Handler>();
        services.AddSingleton<ListClients.Handler>();
        services.AddSingleton<UpdateClient.Handler>();
        services.AddSingleton<DeleteClient.Handler>();

        services.AddSingleton<IClientLookup, ClientLookup>();

        services.AddTransient<ClientsViewModel>();
        services.AddSingleton<IModulePage, ClientsPage>();
        services.AddSingleton<IStartupTask, MigrateClientsDatabaseTask>();

        return services;
    }
}
