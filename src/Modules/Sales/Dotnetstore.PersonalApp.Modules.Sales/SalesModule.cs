using Dotnetstore.PersonalApp.Modules.Sales.Features.Credits;
using Dotnetstore.PersonalApp.Modules.Sales.Features.Customers;
using Dotnetstore.PersonalApp.Modules.Sales.Features.Invoices;
using Dotnetstore.PersonalApp.Modules.Sales.Features.Pdf;
using Dotnetstore.PersonalApp.Modules.Sales.Features.Offers;
using Dotnetstore.PersonalApp.Modules.Sales.Features.Orders;
using Dotnetstore.PersonalApp.Modules.Sales.Features.PickLists;
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
        services.AddSingleton<GetCustomerSalesHistory.Handler>();
        services.AddSingleton<GetCustomerPhoto.Handler>();
        services.AddSingleton<SetCustomerPhoto.Handler>();
        services.AddSingleton<CustomerPhotoPanelFactory>();

        services.AddSingleton<IValidator<CreateOffer.Command>, CreateOffer.Validator>();
        services.AddSingleton<IValidator<UpdateOffer.Command>, UpdateOffer.Validator>();
        services.AddSingleton<CreateOffer.Handler>();
        services.AddSingleton<UpdateOffer.Handler>();
        services.AddSingleton<ChangeOfferStatus.Handler>();
        services.AddSingleton<DeleteOffer.Handler>();
        services.AddSingleton<GetOffer.Handler>();
        services.AddSingleton<ListOffers.Handler>();

        services.AddSingleton<CreateOrderFromOffer.Handler>();
        services.AddSingleton<GetOrder.Handler>();
        services.AddSingleton<ListOrders.Handler>();
        services.AddSingleton<CancelOrder.Handler>();
        services.AddSingleton<GetPickList.Handler>();
        services.AddSingleton<ListPickLists.Handler>();
        services.AddSingleton<SavePickedQuantities.Handler>();

        services.AddSingleton<CreateInvoiceFromOrder.Handler>();
        services.AddSingleton<GetInvoice.Handler>();
        services.AddSingleton<ListInvoices.Handler>();
        services.AddSingleton<RecordPayment.Handler>();
        services.AddSingleton<ApplyCredit.Handler>();
        services.AddSingleton<DeletePayment.Handler>();

        services.AddSingleton<ListCredits.Handler>();
        services.AddSingleton<GetCustomerCredit.Handler>();
        services.AddSingleton<RefundCredit.Handler>();
        services.AddSingleton<CancelInvoice.Handler>();
        services.AddSingleton<ExportSalesPdf.Handler>();
        services.AddSingleton<SalesPdfSaver>();

        services.AddTransient<OffersViewModel>();
        services.AddTransient<OrdersViewModel>();
        services.AddTransient<PickListsViewModel>();
        services.AddTransient<InvoicesViewModel>();
        services.AddTransient<CreditsViewModel>();
        services.AddTransient<CustomersViewModel>();
        services.AddSingleton<IModulePage, OffersPage>();
        services.AddSingleton<IModulePage, OrdersPage>();
        services.AddSingleton<IModulePage, PickListsPage>();
        services.AddSingleton<IModulePage, InvoicesPage>();
        services.AddSingleton<IModulePage, CreditsPage>();
        services.AddSingleton<IModulePage, CustomersPage>();

        services.AddSingleton(new ModuleInfo("sales", "Sales_Title"));
        services.AddSingleton<IStartupTask, MigrateSalesDatabaseTask>();

        return services;
    }
}