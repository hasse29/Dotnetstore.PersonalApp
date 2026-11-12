using Dotnetstore.PersonalApp.Host.Backup;
using Dotnetstore.PersonalApp.Host.Resources;
using Dotnetstore.PersonalApp.Host.Startup;
using Dotnetstore.PersonalApp.Host.ViewModels;
using Dotnetstore.PersonalApp.Host.Auth;
using Dotnetstore.PersonalApp.Host.Events;
using Dotnetstore.PersonalApp.Host.Updates;
using Dotnetstore.PersonalApp.Host.Files;
using Dotnetstore.PersonalApp.Shared.Files;
using Dotnetstore.PersonalApp.Modules.Audit;
using Dotnetstore.PersonalApp.Modules.Clients;
using Dotnetstore.PersonalApp.Modules.Cvs;
using Dotnetstore.PersonalApp.Modules.Grades;
using Dotnetstore.PersonalApp.Modules.Schedule;
using Dotnetstore.PersonalApp.Modules.Students;
using Dotnetstore.PersonalApp.Modules.Subjects;
using Dotnetstore.PersonalApp.Modules.Email;
using Dotnetstore.PersonalApp.Modules.Contact;
using Dotnetstore.PersonalApp.Modules.Products;
using Dotnetstore.PersonalApp.Modules.Sales;
using Dotnetstore.PersonalApp.Modules.TimeReports;
using Dotnetstore.PersonalApp.Modules.Users;
using Dotnetstore.PersonalApp.Shared.Auth;
using Dotnetstore.PersonalApp.Shared.Contracts.Events;
using Dotnetstore.PersonalApp.Shared.Events;
using Dotnetstore.PersonalApp.Shared.Audit;
using Dotnetstore.PersonalApp.Shared.Localization;
using Dotnetstore.PersonalApp.Shared.Shell;
using Dotnetstore.PersonalApp.Shared.Startup;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;

namespace Dotnetstore.PersonalApp.Host;

internal static class CompositionRoot
{
    public static ServiceProvider BuildServices(string? dataDirectory = null)
    {
        dataDirectory ??= AppPaths.DataDirectory;
        Directory.CreateDirectory(dataDirectory);

        Localizer.Instance.AddResources(HostStrings.ResourceManager);

        var services = new ServiceCollection();

        services.AddSingleton(TimeProvider.System);
        services.AddLogging(logging => logging.ClearProviders().AddSerilog(dispose: false));
        services.AddSingleton<ILocalizationService>(Localizer.Instance);
        services.AddSingleton<IFileSaveService, AvaloniaFileSaveService>();
        services.AddSingleton<IFileOpenService, AvaloniaFileOpenService>();
        services.AddSingleton<IFilePickService, AvaloniaFilePickService>();
        services.AddSingleton(sp => new DatabaseBackup(dataDirectory, sp.GetRequiredService<TimeProvider>()));
        services.AddSingleton<IAppRestarter, AvaloniaAppRestarter>();
        services.AddHttpClient(GitHubUpdateChecker.ClientName);
        services.AddSingleton<IUpdateChecker, GitHubUpdateChecker>();
        services.AddSingleton<IUriLauncher, AvaloniaUriLauncher>();
        services.AddSingleton(new UpdateOptions(UserSettings.Load(dataDirectory).CheckForUpdates));
        services.AddSingleton<UpdateBannerViewModel>();
        services.AddSingleton<CurrentUserAccessor>();
        services.AddSingleton<ICurrentUser>(sp => sp.GetRequiredService<CurrentUserAccessor>());
        services.AddSingleton(new ModuleSettings(dataDirectory));
        services.AddSingleton<IModuleAvailability>(sp => sp.GetRequiredService<ModuleSettings>());
        // Students, Subjects, Schedule, Grades and Tests are one "School" module in the list, so it is described here rather than by each of them.
        services.AddSingleton(new ModuleInfo(SchoolGroup.ModuleKey, "Shell_School"));
        services.AddSingleton<IModulePage, DashboardPage>();
        services.AddTransient<DashboardViewModel>();
        services.AddSingleton<IModulePage, ModulesPage>();
        services.AddTransient<ModulesViewModel>();
        services.AddSingleton<IModulePage, BackupPage>();
        services.AddTransient<BackupViewModel>();
        services.AddSingleton<MainWindowViewModel>();
        services.AddSingleton<StartupRunner>();
        services.AddSingleton<IStartupTask>(sp => new RemoveRetiredDatabasesTask(dataDirectory, sp.GetRequiredService<ILocalizationService>()));
        services.AddSingleton<IEventPublisher, InProcessEventPublisher>();
        services.AddSingleton<UserPhotoNotifier>();
        services.AddSingleton<IIntegrationEventConsumer<UserPhotoChanged>>(sp => sp.GetRequiredService<UserPhotoNotifier>());

        services.AddClientsModule(Path.Combine(dataDirectory, DatabaseFiles.Clients));
        services.AddTimeReportsModule(Path.Combine(dataDirectory, DatabaseFiles.TimeReports));
        services.AddProductsModule(Path.Combine(dataDirectory, DatabaseFiles.Products));
        services.AddSalesModule(Path.Combine(dataDirectory, DatabaseFiles.Sales));
        services.AddContactModule(
            Path.Combine(dataDirectory, DatabaseFiles.Contact),
            [Path.Combine(dataDirectory, DatabaseFiles.Addresses), Path.Combine(dataDirectory, DatabaseFiles.EmailAddresses), Path.Combine(dataDirectory, DatabaseFiles.PhoneNumbers)]);
        services.AddCvsModule(Path.Combine(dataDirectory, DatabaseFiles.Cvs));
        services.AddStudentsModule(Path.Combine(dataDirectory, DatabaseFiles.Students));
        services.AddSubjectsModule(Path.Combine(dataDirectory, DatabaseFiles.Subjects));
        services.AddScheduleModule(Path.Combine(dataDirectory, DatabaseFiles.Schedule));
        services.AddGradesModule(Path.Combine(dataDirectory, DatabaseFiles.Grades));
        services.AddEmailModule(Path.Combine(dataDirectory, DatabaseFiles.Email));
        services.AddUsersModule(Path.Combine(dataDirectory, DatabaseFiles.Users));
        services.AddAuditModule(Path.Combine(dataDirectory, DatabaseFiles.Audit));

        var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true, ValidateOnBuild = true });

        // Every handler records itself through AuditTrail; connect it to the audit module and to whoever is signed in.
        var currentUser = provider.GetRequiredService<ICurrentUser>();
        AuditTrail.CurrentUser = () => currentUser.User;
        AuditTrail.Clock = provider.GetRequiredService<TimeProvider>();
        AuditTrail.Sink = provider.GetRequiredService<IAuditSink>();
        return provider;
    }
}
