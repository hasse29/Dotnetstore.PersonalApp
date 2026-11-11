using Dotnetstore.PersonalApp.Modules.Sales.Features.Customers;
using Dotnetstore.PersonalApp.Modules.Sales.Features.Offers;
using Dotnetstore.PersonalApp.Modules.Sales.Persistence;
using Dotnetstore.PersonalApp.Modules.Sales.Resources;
using Dotnetstore.PersonalApp.Modules.Sales.Startup;
using Dotnetstore.PersonalApp.Shared.Localization;
using Dotnetstore.PersonalApp.Shared.Shell;
using Dotnetstore.PersonalApp.Shared.Startup;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Dotnetstore.PersonalApp.Modules.Sales;

public static class SalesModule
{
    public static IServiceCollection AddSalesModule(this IServiceCollection services, string databasePath)
    {
        Localizer.Instance.AddResources(SalesStrings.ResourceManager);

        services.AddDbContextFactory<SalesDbContext>(options => options.UseSqlite($"Data Source={databasePath}"));

        services.AddSingleton<IValidator<CreateCustomer.Command>, CreateCustomer.Validator>();
        services.AddSingleton<IValidator<UpdateCustomer.Command>, UpdateCustomer.Validator>();
        services.AddSingleton<CreateCustomer.Handler>();
        services.AddSingleton<UpdateCustomer.Handler>();
        services.AddSingleton<DeleteCustomer.Handler>();
        services.AddSingleton<ListCustomers.Handler>();

        services.AddSingleton<IValidator<CreateOffer.Command>, CreateOffer.Validator>();
        services.AddSingleton<IValidator<UpdateOffer.Command>, UpdateOffer.Validator>();
        services.AddSingleton<CreateOffer.Handler>();
        services.AddSingleton<UpdateOffer.Handler>();
        services.AddSingleton<ChangeOfferStatus.Handler>();
        services.AddSingleton<DeleteOffer.Handler>();
        services.AddSingleton<GetOffer.Handler>();
        services.AddSingleton<ListOffers.Handler>();

        services.AddTransient<OffersViewModel>();
        services.AddTransient<CustomersViewModel>();
        services.AddSingleton<IModulePage, OffersPage>();
        services.AddSingleton<IModulePage, CustomersPage>();

        services.AddSingleton(new ModuleInfo("sales", "Sales_Title"));
        services.AddSingleton<IStartupTask, MigrateSalesDatabaseTask>();

        return services;
    }
}