using Dotnetstore.PersonalApp.Modules.Users.Domain;
using Dotnetstore.PersonalApp.Modules.Users.Features.Authentication;
using Dotnetstore.PersonalApp.Modules.Users.Features.Users;
using Dotnetstore.PersonalApp.Modules.Users.Persistence;
using Dotnetstore.PersonalApp.Modules.Users.Startup;
using Dotnetstore.PersonalApp.Modules.Users.Resources;
using Dotnetstore.PersonalApp.Shared.Auth;
using Dotnetstore.PersonalApp.Shared.Localization;
using Dotnetstore.PersonalApp.Shared.Shell;
using Dotnetstore.PersonalApp.Shared.Startup;
using FluentValidation;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Dotnetstore.PersonalApp.Modules.Users;

public static class UsersModule
{
    public static IServiceCollection AddUsersModule(this IServiceCollection services, string databasePath)
    {
        Localizer.Instance.AddResources(UsersStrings.ResourceManager);

        services.AddDbContextFactory<UsersDbContext>(options => options.UseSqlite($"Data Source={databasePath}"));
        services.AddSingleton<IPasswordHasher<User>, PasswordHasher<User>>();

        services.AddSingleton<IValidator<CreateUser.Command>, CreateUser.Validator>();
        services.AddSingleton<IValidator<UpdateUser.Command>, UpdateUser.Validator>();

        services.AddSingleton<CreateUser.Handler>();
        services.AddSingleton<GetUser.Handler>();
        services.AddSingleton<ListUsers.Handler>();
        services.AddSingleton<UpdateUser.Handler>();
        services.AddSingleton<DeleteUser.Handler>();

        services.AddSingleton<Login.Handler>();
        services.AddSingleton<HasUsers.Handler>();
        services.AddSingleton<IAuthenticationPages, AuthenticationPages>();

        services.AddTransient<UsersViewModel>();
        services.AddSingleton<IModulePage, UsersPage>();
        services.AddSingleton(new ModuleInfo("users", "Users_Title", CanDisable: false));
        services.AddSingleton<IStartupTask, MigrateUsersDatabaseTask>();

        return services;
    }
}
