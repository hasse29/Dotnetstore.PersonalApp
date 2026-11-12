using Dotnetstore.PersonalApp.Modules.Contact.Addresses;
using Dotnetstore.PersonalApp.Modules.Contact.Addresses.Features.Addresses;
using Dotnetstore.PersonalApp.Modules.Contact.Addresses.Features.AddressTypes;
using Dotnetstore.PersonalApp.Modules.Contact.Consumers;
using Dotnetstore.PersonalApp.Modules.Contact.EmailAddresses;
using Dotnetstore.PersonalApp.Modules.Contact.EmailAddresses.Features.EmailAddresses;
using Dotnetstore.PersonalApp.Modules.Contact.EmailAddresses.Features.EmailAddressTypes;
using Dotnetstore.PersonalApp.Modules.Contact.Persistence;
using Dotnetstore.PersonalApp.Modules.Contact.PhoneNumbers;
using Dotnetstore.PersonalApp.Modules.Contact.PhoneNumbers.Features.PhoneNumbers;
using Dotnetstore.PersonalApp.Modules.Contact.PhoneNumbers.Features.PhoneNumberTypes;
using Dotnetstore.PersonalApp.Modules.Contact.Resources;
using Dotnetstore.PersonalApp.Modules.Contact.Startup;
using Dotnetstore.PersonalApp.Shared.Addresses;
using Dotnetstore.PersonalApp.Shared.Contacts;
using Dotnetstore.PersonalApp.Shared.Contracts.Events;
using Dotnetstore.PersonalApp.Shared.EmailAddresses;
using Dotnetstore.PersonalApp.Shared.Events;
using Dotnetstore.PersonalApp.Shared.Localization;
using Dotnetstore.PersonalApp.Shared.PhoneNumbers;
using Dotnetstore.PersonalApp.Shared.Shell;
using Dotnetstore.PersonalApp.Shared.Startup;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Dotnetstore.PersonalApp.Modules.Contact;

public static class ContactModule
{
    internal const string Key = "contact";

    /// <param name="services">The service collection.</param>
    /// <param name="databasePath">The Contact database.</param>
    /// <param name="legacyDatabasePaths">The databases of the former Addresses, EmailAddresses and PhoneNumbers modules. Their rows are copied into the Contact database once, then the files are deleted.</param>
    public static IServiceCollection AddContactModule(this IServiceCollection services, string databasePath, IReadOnlyList<string>? legacyDatabasePaths = null)
    {
        Localizer.Instance.AddResources(ContactStrings.ResourceManager);

        services.AddDbContextFactory<ContactDbContext>(options => options.UseSqlite($"Data Source={databasePath}"));

        // Addresses
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
        services.AddSingleton<IContactInfoSource, AddressContactSource>();


        services.AddTransient<AddressTypesViewModel>();
        services.AddSingleton<IModulePage, AddressesPage>();

        // Email addresses
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


        services.AddTransient<EmailAddressTypesViewModel>();
        services.AddSingleton<IModulePage, EmailAddressesPage>();

        // Phone numbers
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
        services.AddSingleton<IContactInfoSource, PhoneContactSource>();


        services.AddTransient<PhoneNumberTypesViewModel>();
        services.AddSingleton<IModulePage, PhoneNumbersPage>();

        services.AddSingleton<OwnerDeletedConsumer>();
        services.AddSingleton<IIntegrationEventConsumer<UserDeleted>>(sp => sp.GetRequiredService<OwnerDeletedConsumer>());
        services.AddSingleton<IIntegrationEventConsumer<ClientDeleted>>(sp => sp.GetRequiredService<OwnerDeletedConsumer>());
        services.AddSingleton<IIntegrationEventConsumer<ContactPersonDeleted>>(sp => sp.GetRequiredService<OwnerDeletedConsumer>());
        services.AddSingleton<IIntegrationEventConsumer<CustomerDeleted>>(sp => sp.GetRequiredService<OwnerDeletedConsumer>());
        services.AddSingleton<IIntegrationEventConsumer<StudentDeleted>>(sp => sp.GetRequiredService<OwnerDeletedConsumer>());

        services.AddSingleton(new ModuleInfo(Key, "ContactPanel_Title"));
        services.AddSingleton<IStartupTask>(sp => new MigrateContactDatabaseTask(
            sp.GetRequiredService<IDbContextFactory<ContactDbContext>>(),
            sp.GetRequiredService<ILocalizationService>(),
            legacyDatabasePaths ?? []));

        return services;
    }
}
