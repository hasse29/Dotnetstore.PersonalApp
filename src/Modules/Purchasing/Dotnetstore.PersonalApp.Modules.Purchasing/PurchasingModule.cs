using Dotnetstore.PersonalApp.Modules.Purchasing.Consumers;
using Dotnetstore.PersonalApp.Modules.Purchasing.Features.ProductVendors;
using Dotnetstore.PersonalApp.Modules.Purchasing.Features.PurchaseOrderDetails;
using Dotnetstore.PersonalApp.Modules.Purchasing.Features.PurchaseOrders;
using Dotnetstore.PersonalApp.Modules.Purchasing.Features.ShipMethods;
using Dotnetstore.PersonalApp.Modules.Purchasing.Features.Vendors;
using Dotnetstore.PersonalApp.Modules.Purchasing.Features;
using Dotnetstore.PersonalApp.Modules.Purchasing.Persistence;
using Dotnetstore.PersonalApp.Modules.Purchasing.Resources;
using Dotnetstore.PersonalApp.Modules.Purchasing.Startup;
using Dotnetstore.PersonalApp.Shared.Clients;
using Dotnetstore.PersonalApp.Shared.Products;
using Dotnetstore.PersonalApp.Shared.Purchasing;
using Dotnetstore.PersonalApp.Shared.Users;
using Dotnetstore.PersonalApp.Shared.Contracts.Events;
using Dotnetstore.PersonalApp.Shared.Events;
using Dotnetstore.PersonalApp.Shared.Localization;
using Dotnetstore.PersonalApp.Shared.Shell;
using Dotnetstore.PersonalApp.Shared.Startup;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Dotnetstore.PersonalApp.Modules.Purchasing;

public static class PurchasingModule
{
    public static IServiceCollection AddPurchasingModule(this IServiceCollection services, string databasePath)
    {
        Localizer.Instance.AddResources(PurchasingStrings.ResourceManager);

        services.AddDbContextFactory<PurchasingDbContext>(options => options.UseSqlite($"Data Source={databasePath}"));

        AddFeatures(services);
        services.AddSingleton<IShipMethodLookup, ShipMethodLookup>();

        services.AddSingleton(new ModuleInfo(PurchasingGroup.ModuleKey, "Purchasing_Title"));
        services.AddSingleton<IModulePage>(sp => new EntityPage<ShipMethodsViewModel>(sp, "ShipMethods_Title"));
        services.AddSingleton<IModulePage>(sp => new EntityPage<VendorsViewModel>(sp, "Vendors_Title"));
        services.AddSingleton<IModulePage>(sp => new EntityPage<ProductVendorsViewModel>(sp, "ProductVendors_Title"));
        services.AddSingleton<IModulePage>(sp => new EntityPage<PurchaseOrdersViewModel>(sp, "PurchaseOrders_Title"));
        services.AddSingleton<IModulePage>(sp => new EntityPage<PurchaseOrderDetailsViewModel>(sp, "PurchaseOrderDetails_Title"));
        services.AddSingleton<IStartupTask, MigratePurchasingDatabaseTask>();

        return services;
    }

    /// <summary>Handlers, validators and view models; separate so tests can build them without a database file.</summary>
    internal static void AddFeatures(IServiceCollection services)
    {
        services.AddSingleton<IValidator<CreateShipMethod.Command>, CreateShipMethod.Validator>();
        services.AddSingleton<IValidator<UpdateShipMethod.Command>, UpdateShipMethod.Validator>();
        services.AddSingleton<CreateShipMethod.Handler>();
        services.AddSingleton<UpdateShipMethod.Handler>();
        services.AddSingleton<DeleteShipMethod.Handler>();
        services.AddSingleton<ListShipMethods.Handler>();
        services.AddSingleton<ListShipMethodOptions.Handler>();
        services.AddTransient<ShipMethodsViewModel>();

        services.AddSingleton<IValidator<CreateVendor.Command>, CreateVendor.Validator>();
        services.AddSingleton<IValidator<UpdateVendor.Command>, UpdateVendor.Validator>();
        services.AddSingleton<CreateVendor.Handler>();
        services.AddSingleton<UpdateVendor.Handler>();
        services.AddSingleton<DeleteVendor.Handler>();
        services.AddSingleton<ListVendors.Handler>();
        services.AddSingleton<ListVendorOptions.Handler>();
        services.AddTransient<VendorsViewModel>();
        services.AddTransient<IDetailFactory<VendorDto>, DetailFactory<VendorDto, VendorDetailViewModel>>();

        services.AddSingleton<IValidator<CreateProductVendor.Command>, CreateProductVendor.Validator>();
        services.AddSingleton<IValidator<UpdateProductVendor.Command>, UpdateProductVendor.Validator>();
        services.AddSingleton<CreateProductVendor.Handler>();
        services.AddSingleton<UpdateProductVendor.Handler>();
        services.AddSingleton<DeleteProductVendor.Handler>();
        services.AddSingleton<ListProductVendors.Handler>();
        services.AddTransient<ProductVendorsViewModel>();

        services.AddSingleton<IValidator<CreatePurchaseOrder.Command>, CreatePurchaseOrder.Validator>();
        services.AddSingleton<IValidator<UpdatePurchaseOrder.Command>, UpdatePurchaseOrder.Validator>();
        services.AddSingleton<CreatePurchaseOrder.Handler>();
        services.AddSingleton<UpdatePurchaseOrder.Handler>();
        services.AddSingleton<DeletePurchaseOrder.Handler>();
        services.AddSingleton<ListPurchaseOrders.Handler>();
        services.AddSingleton<ListPurchaseOrderOptions.Handler>();
        services.AddTransient<PurchaseOrdersViewModel>();
        services.AddTransient<IDetailFactory<PurchaseOrderDto>, DetailFactory<PurchaseOrderDto, PurchaseOrderScreenViewModel>>();

        services.AddSingleton<IValidator<CreatePurchaseOrderDetail.Command>, CreatePurchaseOrderDetail.Validator>();
        services.AddSingleton<IValidator<UpdatePurchaseOrderDetail.Command>, UpdatePurchaseOrderDetail.Validator>();
        services.AddSingleton<CreatePurchaseOrderDetail.Handler>();
        services.AddSingleton<UpdatePurchaseOrderDetail.Handler>();
        services.AddSingleton<DeletePurchaseOrderDetail.Handler>();
        services.AddSingleton<ListPurchaseOrderDetails.Handler>();
        services.AddTransient<PurchaseOrderDetailsViewModel>();

        services.AddSingleton<ExternalReferences>();
        services.AddSingleton<IIntegrationEventConsumer<ClientDeleted>, ClientDeletedConsumer>();
        services.AddSingleton<IIntegrationEventConsumer<ProductDeleted>, ProductDeletedConsumer>();
        services.AddSingleton<IIntegrationEventConsumer<UserDeleted>, UserDeletedConsumer>();
    }
}