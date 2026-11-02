using Dotnetstore.PersonalApp.Modules.Email.Consumers;
using Dotnetstore.PersonalApp.Modules.Email.Features.EmailAccounts;
using Dotnetstore.PersonalApp.Modules.Email.Features.Messages;
using Dotnetstore.PersonalApp.Modules.Email.Mail;
using Dotnetstore.PersonalApp.Modules.Email.Persistence;
using Dotnetstore.PersonalApp.Modules.Email.Resources;
using Dotnetstore.PersonalApp.Modules.Email.Security;
using Dotnetstore.PersonalApp.Modules.Email.Startup;
using Dotnetstore.PersonalApp.Shared.Contracts.Events;
using Dotnetstore.PersonalApp.Shared.Email;
using Dotnetstore.PersonalApp.Shared.Events;
using Dotnetstore.PersonalApp.Shared.Localization;
using Dotnetstore.PersonalApp.Shared.Shell;
using Dotnetstore.PersonalApp.Shared.Startup;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Dotnetstore.PersonalApp.Modules.Email;

public static class EmailModule
{
    public static IServiceCollection AddEmailModule(this IServiceCollection services, string databasePath)
    {
        // Mail passwords are encrypted with Windows DPAPI; the app ships as a Windows installer.
        if (!OperatingSystem.IsWindows())
        {
            throw new PlatformNotSupportedException("The Email module stores passwords with Windows DPAPI and only runs on Windows.");
        }

        Localizer.Instance.AddResources(EmailStrings.ResourceManager);

        services.AddDbContextFactory<EmailDbContext>(options => options.UseSqlite($"Data Source={databasePath}"));

        services.AddSingleton<ISecretProtector, DpapiSecretProtector>();

        services.AddSingleton<IValidator<CreateEmailAccount.Command>, CreateEmailAccount.Validator>();
        services.AddSingleton<IValidator<UpdateEmailAccount.Command>, UpdateEmailAccount.Validator>();

        services.AddSingleton<CreateEmailAccount.Handler>();
        services.AddSingleton<GetEmailAccount.Handler>();
        services.AddSingleton<ListEmailAccounts.Handler>();
        services.AddSingleton<UpdateEmailAccount.Handler>();
        services.AddSingleton<DeleteEmailAccount.Handler>();
        services.AddSingleton<DeleteEmailAccountsForUser.Handler>();
        services.AddSingleton<IValidator<TestEmailConnection.Command>, TestEmailConnection.Validator>();
        services.AddSingleton<TestEmailConnection.Handler>();

        services.AddSingleton<IMailGateway, MailKitGateway>();
        services.AddSingleton<IValidator<SendEmail.Command>, SendEmail.Validator>();
        services.AddSingleton<SendEmail.Handler>();
        services.AddSingleton<ISystemMailSender, SystemMailSender>();
        services.AddSingleton<FetchEmail.Handler>();
        services.AddSingleton<ListEmailMessages.Handler>();
        services.AddSingleton<GetEmailMessage.Handler>();
        services.AddSingleton<DeleteEmailMessage.Handler>();
        services.AddSingleton<GetEmailAttachment.Handler>();
        services.AddSingleton<IValidator<SaveDraft.Command>, SaveDraft.Validator>();
        services.AddSingleton<SaveDraft.Handler>();
        services.AddSingleton<ListDrafts.Handler>();
        services.AddSingleton<GetDraft.Handler>();
        services.AddSingleton<DeleteDraft.Handler>();
        services.AddSingleton<ListSentMessages.Handler>();
        services.AddSingleton<GetSentMessage.Handler>();
        services.AddSingleton<DeleteSentMessage.Handler>();

        services.AddSingleton<IIntegrationEventConsumer<UserDeleted>, UserDeletedConsumer>();

        services.AddSingleton<IModulePage, InboxPage>();
        services.AddSingleton<IModulePage, ComposePage>();
        services.AddSingleton<IModulePage, DraftsPage>();
        services.AddSingleton<IModulePage, SentPage>();
        services.AddSingleton<IModulePage, EmailAccountsPage>();
        services.AddSingleton(new ModuleInfo("email", "Email_Module"));
        services.AddSingleton<IStartupTask, MigrateEmailDatabaseTask>();

        return services;
    }
}
