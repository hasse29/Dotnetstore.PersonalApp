using Dotnetstore.PersonalApp.Modules.Audit.Features.Entries;
using Dotnetstore.PersonalApp.Modules.Audit.Persistence;
using Dotnetstore.PersonalApp.Modules.Audit.Resources;
using Dotnetstore.PersonalApp.Modules.Audit.Startup;
using Dotnetstore.PersonalApp.Shared.Audit;
using Dotnetstore.PersonalApp.Shared.Localization;
using Dotnetstore.PersonalApp.Shared.Shell;
using Dotnetstore.PersonalApp.Shared.Startup;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Dotnetstore.PersonalApp.Modules.Audit;

public static class AuditModule
{
    public const string Key = "audit";

    public static IServiceCollection AddAuditModule(this IServiceCollection services, string databasePath)
    {
        Localizer.Instance.AddResources(AuditStrings.ResourceManager);

        services.AddDbContextFactory<AuditDbContext>(options => options.UseSqlite($"Data Source={databasePath}"));

        // The sink is a singleton the Host connects to AuditTrail once the container is built; disposing the container flushes it.
        services.AddSingleton<AuditWriter>();
        services.AddSingleton<IAuditSink>(sp => sp.GetRequiredService<AuditWriter>());

        services.AddSingleton<ListAuditEntries.Handler>();

        services.AddTransient<AuditViewModel>();
        services.AddSingleton<IModulePage, AuditPage>();
        services.AddSingleton(new ModuleInfo(Key, "Audit_Title", CanDisable: false));
        services.AddSingleton<IStartupTask, MigrateAuditDatabaseTask>();

        return services;
    }
}
