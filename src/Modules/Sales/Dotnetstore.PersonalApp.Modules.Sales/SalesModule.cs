using Dotnetstore.PersonalApp.Modules.Sales.Consumers;
using Dotnetstore.PersonalApp.Modules.Sales.Features.CountryRegionCurrencies;
using Dotnetstore.PersonalApp.Modules.Sales.Features.Currencies;
using Dotnetstore.PersonalApp.Modules.Sales.Features.CurrencyRates;
using Dotnetstore.PersonalApp.Modules.Sales.Features.Customers;
using Dotnetstore.PersonalApp.Modules.Sales.Features.SalesOrderDetails;
using Dotnetstore.PersonalApp.Modules.Sales.Features.SalesOrderReasons;
using Dotnetstore.PersonalApp.Modules.Sales.Features.SalesOrders;
using Dotnetstore.PersonalApp.Modules.Sales.Features.SalesPeople;
using Dotnetstore.PersonalApp.Modules.Sales.Features.SalesPersonQuotaHistories;
using Dotnetstore.PersonalApp.Modules.Sales.Features.SalesReasons;
using Dotnetstore.PersonalApp.Modules.Sales.Features.SalesTaxRates;
using Dotnetstore.PersonalApp.Modules.Sales.Features.SalesTerritories;
using Dotnetstore.PersonalApp.Modules.Sales.Features.SalesTerritoryHistories;
using Dotnetstore.PersonalApp.Modules.Sales.Features.SpecialOfferProducts;
using Dotnetstore.PersonalApp.Modules.Sales.Features.SpecialOffers;
using Dotnetstore.PersonalApp.Modules.Sales.Features.Stores;
using Dotnetstore.PersonalApp.Modules.Sales.Features;
using Dotnetstore.PersonalApp.Modules.Sales.Persistence;
using Dotnetstore.PersonalApp.Modules.Sales.Resources;
using Dotnetstore.PersonalApp.Modules.Sales.Startup;
using Dotnetstore.PersonalApp.Shared.Clients;
using Dotnetstore.PersonalApp.Shared.Products;
using Dotnetstore.PersonalApp.Shared.Purchasing;
using Dotnetstore.PersonalApp.Shared.Sales;
using Dotnetstore.PersonalApp.Shared.Users;
using Dotnetstore.PersonalApp.Shared.Contracts.Events;
using Dotnetstore.PersonalApp.Shared.Events;
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

        AddFeatures(services);


        services.AddSingleton(new ModuleInfo(SalesGroup.ModuleKey, "Sales_Title"));
        services.AddSingleton<IModulePage>(sp => new EntityPage<SalesTerritoriesViewModel>(sp, "SalesTerritories_Title"));
        services.AddSingleton<IModulePage>(sp => new EntityPage<SalesPeopleViewModel>(sp, "SalesPeople_Title"));
        services.AddSingleton<IModulePage>(sp => new EntityPage<SalesPersonQuotaHistoriesViewModel>(sp, "SalesPersonQuotaHistories_Title"));
        services.AddSingleton<IModulePage>(sp => new EntityPage<SalesTerritoryHistoriesViewModel>(sp, "SalesTerritoryHistories_Title"));
        services.AddSingleton<IModulePage>(sp => new EntityPage<SalesTaxRatesViewModel>(sp, "SalesTaxRates_Title"));
        services.AddSingleton<IModulePage>(sp => new EntityPage<CurrenciesViewModel>(sp, "Currencies_Title"));
        services.AddSingleton<IModulePage>(sp => new EntityPage<CurrencyRatesViewModel>(sp, "CurrencyRates_Title"));
        services.AddSingleton<IModulePage>(sp => new EntityPage<CountryRegionCurrenciesViewModel>(sp, "CountryRegionCurrencies_Title"));
        services.AddSingleton<IModulePage>(sp => new EntityPage<SalesReasonsViewModel>(sp, "SalesReasons_Title"));
        services.AddSingleton<IModulePage>(sp => new EntityPage<SpecialOffersViewModel>(sp, "SpecialOffers_Title"));
        services.AddSingleton<IModulePage>(sp => new EntityPage<SpecialOfferProductsViewModel>(sp, "SpecialOfferProducts_Title"));
        services.AddSingleton<IModulePage>(sp => new EntityPage<StoresViewModel>(sp, "Stores_Title"));
        services.AddSingleton<IModulePage>(sp => new EntityPage<CustomersViewModel>(sp, "Customers_Title"));
        services.AddSingleton<IModulePage>(sp => new EntityPage<SalesOrdersViewModel>(sp, "SalesOrders_Title"));
        services.AddSingleton<IModulePage>(sp => new EntityPage<SalesOrderDetailsViewModel>(sp, "SalesOrderDetails_Title"));
        services.AddSingleton<IModulePage>(sp => new EntityPage<SalesOrderReasonsViewModel>(sp, "SalesOrderReasons_Title"));
        services.AddSingleton<IStartupTask, MigrateSalesDatabaseTask>();

        return services;
    }

    /// <summary>Handlers, validators and view models; separate so tests can build them without a database file.</summary>
    internal static void AddFeatures(IServiceCollection services)
    {
        services.AddSingleton<IValidator<CreateSalesTerritory.Command>, CreateSalesTerritory.Validator>();
        services.AddSingleton<IValidator<UpdateSalesTerritory.Command>, UpdateSalesTerritory.Validator>();
        services.AddSingleton<CreateSalesTerritory.Handler>();
        services.AddSingleton<UpdateSalesTerritory.Handler>();
        services.AddSingleton<DeleteSalesTerritory.Handler>();
        services.AddSingleton<ListSalesTerritories.Handler>();
        services.AddSingleton<ListSalesTerritoryOptions.Handler>();
        services.AddTransient<SalesTerritoriesViewModel>();

        services.AddSingleton<IValidator<CreateSalesPerson.Command>, CreateSalesPerson.Validator>();
        services.AddSingleton<IValidator<UpdateSalesPerson.Command>, UpdateSalesPerson.Validator>();
        services.AddSingleton<CreateSalesPerson.Handler>();
        services.AddSingleton<UpdateSalesPerson.Handler>();
        services.AddSingleton<DeleteSalesPerson.Handler>();
        services.AddSingleton<ListSalesPeople.Handler>();
        services.AddSingleton<ListSalesPersonOptions.Handler>();
        services.AddTransient<SalesPeopleViewModel>();
        services.AddTransient<IDetailFactory<SalesPersonDto>, DetailFactory<SalesPersonDto, SalesPersonDetailViewModel>>();

        services.AddSingleton<IValidator<CreateSalesPersonQuotaHistory.Command>, CreateSalesPersonQuotaHistory.Validator>();
        services.AddSingleton<IValidator<UpdateSalesPersonQuotaHistory.Command>, UpdateSalesPersonQuotaHistory.Validator>();
        services.AddSingleton<CreateSalesPersonQuotaHistory.Handler>();
        services.AddSingleton<UpdateSalesPersonQuotaHistory.Handler>();
        services.AddSingleton<DeleteSalesPersonQuotaHistory.Handler>();
        services.AddSingleton<ListSalesPersonQuotaHistories.Handler>();
        services.AddTransient<SalesPersonQuotaHistoriesViewModel>();

        services.AddSingleton<IValidator<CreateSalesTerritoryHistory.Command>, CreateSalesTerritoryHistory.Validator>();
        services.AddSingleton<IValidator<UpdateSalesTerritoryHistory.Command>, UpdateSalesTerritoryHistory.Validator>();
        services.AddSingleton<CreateSalesTerritoryHistory.Handler>();
        services.AddSingleton<UpdateSalesTerritoryHistory.Handler>();
        services.AddSingleton<DeleteSalesTerritoryHistory.Handler>();
        services.AddSingleton<ListSalesTerritoryHistories.Handler>();
        services.AddTransient<SalesTerritoryHistoriesViewModel>();

        services.AddSingleton<IValidator<CreateSalesTaxRate.Command>, CreateSalesTaxRate.Validator>();
        services.AddSingleton<IValidator<UpdateSalesTaxRate.Command>, UpdateSalesTaxRate.Validator>();
        services.AddSingleton<CreateSalesTaxRate.Handler>();
        services.AddSingleton<UpdateSalesTaxRate.Handler>();
        services.AddSingleton<DeleteSalesTaxRate.Handler>();
        services.AddSingleton<ListSalesTaxRates.Handler>();
        services.AddTransient<SalesTaxRatesViewModel>();

        services.AddSingleton<IValidator<CreateCurrency.Command>, CreateCurrency.Validator>();
        services.AddSingleton<IValidator<UpdateCurrency.Command>, UpdateCurrency.Validator>();
        services.AddSingleton<CreateCurrency.Handler>();
        services.AddSingleton<UpdateCurrency.Handler>();
        services.AddSingleton<DeleteCurrency.Handler>();
        services.AddSingleton<ListCurrencies.Handler>();
        services.AddSingleton<ListCurrencyOptions.Handler>();
        services.AddTransient<CurrenciesViewModel>();

        services.AddSingleton<IValidator<CreateCurrencyRate.Command>, CreateCurrencyRate.Validator>();
        services.AddSingleton<IValidator<UpdateCurrencyRate.Command>, UpdateCurrencyRate.Validator>();
        services.AddSingleton<CreateCurrencyRate.Handler>();
        services.AddSingleton<UpdateCurrencyRate.Handler>();
        services.AddSingleton<DeleteCurrencyRate.Handler>();
        services.AddSingleton<ListCurrencyRates.Handler>();
        services.AddTransient<CurrencyRatesViewModel>();

        services.AddSingleton<IValidator<CreateCountryRegionCurrency.Command>, CreateCountryRegionCurrency.Validator>();
        services.AddSingleton<IValidator<UpdateCountryRegionCurrency.Command>, UpdateCountryRegionCurrency.Validator>();
        services.AddSingleton<CreateCountryRegionCurrency.Handler>();
        services.AddSingleton<UpdateCountryRegionCurrency.Handler>();
        services.AddSingleton<DeleteCountryRegionCurrency.Handler>();
        services.AddSingleton<ListCountryRegionCurrencies.Handler>();
        services.AddTransient<CountryRegionCurrenciesViewModel>();

        services.AddSingleton<IValidator<CreateSalesReason.Command>, CreateSalesReason.Validator>();
        services.AddSingleton<IValidator<UpdateSalesReason.Command>, UpdateSalesReason.Validator>();
        services.AddSingleton<CreateSalesReason.Handler>();
        services.AddSingleton<UpdateSalesReason.Handler>();
        services.AddSingleton<DeleteSalesReason.Handler>();
        services.AddSingleton<ListSalesReasons.Handler>();
        services.AddSingleton<ListSalesReasonOptions.Handler>();
        services.AddTransient<SalesReasonsViewModel>();

        services.AddSingleton<IValidator<CreateSpecialOffer.Command>, CreateSpecialOffer.Validator>();
        services.AddSingleton<IValidator<UpdateSpecialOffer.Command>, UpdateSpecialOffer.Validator>();
        services.AddSingleton<CreateSpecialOffer.Handler>();
        services.AddSingleton<UpdateSpecialOffer.Handler>();
        services.AddSingleton<DeleteSpecialOffer.Handler>();
        services.AddSingleton<ListSpecialOffers.Handler>();
        services.AddSingleton<ListSpecialOfferOptions.Handler>();
        services.AddTransient<SpecialOffersViewModel>();

        services.AddSingleton<IValidator<CreateSpecialOfferProduct.Command>, CreateSpecialOfferProduct.Validator>();
        services.AddSingleton<IValidator<UpdateSpecialOfferProduct.Command>, UpdateSpecialOfferProduct.Validator>();
        services.AddSingleton<CreateSpecialOfferProduct.Handler>();
        services.AddSingleton<UpdateSpecialOfferProduct.Handler>();
        services.AddSingleton<DeleteSpecialOfferProduct.Handler>();
        services.AddSingleton<ListSpecialOfferProducts.Handler>();
        services.AddTransient<SpecialOfferProductsViewModel>();

        services.AddSingleton<IValidator<CreateStore.Command>, CreateStore.Validator>();
        services.AddSingleton<IValidator<UpdateStore.Command>, UpdateStore.Validator>();
        services.AddSingleton<CreateStore.Handler>();
        services.AddSingleton<UpdateStore.Handler>();
        services.AddSingleton<DeleteStore.Handler>();
        services.AddSingleton<ListStores.Handler>();
        services.AddSingleton<ListStoreOptions.Handler>();
        services.AddTransient<StoresViewModel>();

        services.AddSingleton<IValidator<CreateCustomer.Command>, CreateCustomer.Validator>();
        services.AddSingleton<IValidator<UpdateCustomer.Command>, UpdateCustomer.Validator>();
        services.AddSingleton<CreateCustomer.Handler>();
        services.AddSingleton<UpdateCustomer.Handler>();
        services.AddSingleton<DeleteCustomer.Handler>();
        services.AddSingleton<ListCustomers.Handler>();
        services.AddSingleton<ListCustomerOptions.Handler>();
        services.AddTransient<CustomersViewModel>();
        services.AddTransient<IDetailFactory<CustomerDto>, DetailFactory<CustomerDto, CustomerDetailViewModel>>();

        services.AddSingleton<IValidator<CreateSalesOrder.Command>, CreateSalesOrder.Validator>();
        services.AddSingleton<IValidator<UpdateSalesOrder.Command>, UpdateSalesOrder.Validator>();
        services.AddSingleton<CreateSalesOrder.Handler>();
        services.AddSingleton<UpdateSalesOrder.Handler>();
        services.AddSingleton<DeleteSalesOrder.Handler>();
        services.AddSingleton<ListSalesOrders.Handler>();
        services.AddSingleton<ListSalesOrderOptions.Handler>();
        services.AddTransient<SalesOrdersViewModel>();
        services.AddTransient<IDetailFactory<SalesOrderDto>, DetailFactory<SalesOrderDto, SalesOrderScreenViewModel>>();

        services.AddSingleton<IValidator<CreateSalesOrderDetail.Command>, CreateSalesOrderDetail.Validator>();
        services.AddSingleton<IValidator<UpdateSalesOrderDetail.Command>, UpdateSalesOrderDetail.Validator>();
        services.AddSingleton<CreateSalesOrderDetail.Handler>();
        services.AddSingleton<UpdateSalesOrderDetail.Handler>();
        services.AddSingleton<DeleteSalesOrderDetail.Handler>();
        services.AddSingleton<ListSalesOrderDetails.Handler>();
        services.AddTransient<SalesOrderDetailsViewModel>();

        services.AddSingleton<IValidator<CreateSalesOrderReason.Command>, CreateSalesOrderReason.Validator>();
        services.AddSingleton<IValidator<UpdateSalesOrderReason.Command>, UpdateSalesOrderReason.Validator>();
        services.AddSingleton<CreateSalesOrderReason.Handler>();
        services.AddSingleton<UpdateSalesOrderReason.Handler>();
        services.AddSingleton<DeleteSalesOrderReason.Handler>();
        services.AddSingleton<ListSalesOrderReasons.Handler>();
        services.AddTransient<SalesOrderReasonsViewModel>();

        services.AddSingleton<ExternalReferences>();
        services.AddSingleton<IIntegrationEventConsumer<UserDeleted>, UserDeletedConsumer>();
        services.AddSingleton<IIntegrationEventConsumer<ProductDeleted>, ProductDeletedConsumer>();
        services.AddSingleton<IIntegrationEventConsumer<ClientDeleted>, ClientDeletedConsumer>();
        services.AddSingleton<ISalesOrderLookup, SalesOrderLookup>();
    }
}