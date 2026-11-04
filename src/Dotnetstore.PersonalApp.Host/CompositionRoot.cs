using Dotnetstore.PersonalApp.Host.Resources;
using Dotnetstore.PersonalApp.Host.Startup;
using Dotnetstore.PersonalApp.Host.ViewModels;
using Dotnetstore.PersonalApp.Host.Auth;
using Dotnetstore.PersonalApp.Host.Events;
using Dotnetstore.PersonalApp.Host.Updates;
using Dotnetstore.PersonalApp.Host.Files;
using Dotnetstore.PersonalApp.Modules.Addresses;
using Dotnetstore.PersonalApp.Shared.Files;
using Dotnetstore.PersonalApp.Modules.Clients;
using Dotnetstore.PersonalApp.Modules.Cvs;
using Dotnetstore.PersonalApp.Modules.Grades;
using Dotnetstore.PersonalApp.Modules.Schedule;
using Dotnetstore.PersonalApp.Modules.Students;
using Dotnetstore.PersonalApp.Modules.Subjects;
using Dotnetstore.PersonalApp.Modules.Email;
using Dotnetstore.PersonalApp.Modules.EmailAddresses;
using Dotnetstore.PersonalApp.Modules.PhoneNumbers;
using Dotnetstore.PersonalApp.Modules.Production;
using Dotnetstore.PersonalApp.Modules.Sales;
using Dotnetstore.PersonalApp.Modules.Purchasing;
using Dotnetstore.PersonalApp.Modules.Inventory;
using Dotnetstore.PersonalApp.Modules.TimeReports;
using Dotnetstore.PersonalApp.Modules.Users;
using Dotnetstore.PersonalApp.Shared.Auth;
using Dotnetstore.PersonalApp.Shared.Contracts.Events;
using Dotnetstore.PersonalApp.Shared.Events;
using Dotnetstore.PersonalApp.Shared.Localization;
using Dotnetstore.PersonalApp.Shared.Shell;
using Microsoft.Extensions.DependencyInjection;

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
        services.AddSingleton<ILocalizationService>(Localizer.Instance);
        services.AddSingleton<IFileSaveService, AvaloniaFileSaveService>();
        services.AddSingleton<IFileOpenService, AvaloniaFileOpenService>();
        services.AddSingleton<IFilePickService, AvaloniaFilePickService>();
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
        services.AddSingleton<MainWindowViewModel>();
        services.AddSingleton<StartupRunner>();
        services.AddSingleton<IEventPublisher, InProcessEventPublisher>();
        services.AddSingleton<UserPhotoNotifier>();
        services.AddSingleton<IIntegrationEventConsumer<UserPhotoChanged>>(sp => sp.GetRequiredService<UserPhotoNotifier>());

        services.AddClientsModule(Path.Combine(dataDirectory, "clients.db"));
        services.AddTimeReportsModule(Path.Combine(dataDirectory, "timereports.db"));
        services.AddAddressesModule(Path.Combine(dataDirectory, "addresses.db"));
        services.AddEmailAddressesModule(Path.Combine(dataDirectory, "emailaddresses.db"));
        services.AddPhoneNumbersModule(Path.Combine(dataDirectory, "phonenumbers.db"));
        services.AddCvsModule(Path.Combine(dataDirectory, "cvs.db"));
        services.AddStudentsModule(Path.Combine(dataDirectory, "students.db"));
        services.AddSubjectsModule(Path.Combine(dataDirectory, "subjects.db"));
        services.AddScheduleModule(Path.Combine(dataDirectory, "schedule.db"));
        services.AddGradesModule(Path.Combine(dataDirectory, "grades.db"));
        services.AddEmailModule(Path.Combine(dataDirectory, "email.db"));
        services.AddProductionModule(Path.Combine(dataDirectory, "production.db"));
        services.AddSalesModule(Path.Combine(dataDirectory, "sales.db"));
        services.AddPurchasingModule(Path.Combine(dataDirectory, "purchasing.db"));
        services.AddInventoryModule(Path.Combine(dataDirectory, "inventory.db"));
        services.AddUsersModule(Path.Combine(dataDirectory, "users.db"));

        return services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true, ValidateOnBuild = true });
    }
}
