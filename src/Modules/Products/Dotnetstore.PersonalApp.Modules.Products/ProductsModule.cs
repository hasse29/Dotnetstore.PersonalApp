using Dotnetstore.PersonalApp.Modules.Products.Features.ProductCategories;
using Dotnetstore.PersonalApp.Modules.Products.Features.InventoryCounts;
using Dotnetstore.PersonalApp.Modules.Products.Features.Products;
using Dotnetstore.PersonalApp.Modules.Products.Features.Stock;
using Dotnetstore.PersonalApp.Modules.Products.Features.StorageLocations;
using Dotnetstore.PersonalApp.Modules.Products.Features.SupplierPrices;
using Dotnetstore.PersonalApp.Modules.Products.Features.Suppliers;
using Dotnetstore.PersonalApp.Modules.Products.Persistence;
using Dotnetstore.PersonalApp.Modules.Products.Resources;
using Dotnetstore.PersonalApp.Modules.Products.Startup;
using Dotnetstore.PersonalApp.Shared.Localization;
using Dotnetstore.PersonalApp.Shared.Shell;
using Dotnetstore.PersonalApp.Shared.Startup;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Dotnetstore.PersonalApp.Modules.Products;

public static class ProductsModule
{
    public static IServiceCollection AddProductsModule(this IServiceCollection services, string databasePath)
    {
        Localizer.Instance.AddResources(ProductsStrings.ResourceManager);

        services.AddDbContextFactory<ProductsDbContext>(options => options.UseSqlite($"Data Source={databasePath}"));

        services.AddSingleton<IValidator<CreateProduct.Command>, CreateProduct.Validator>();
        services.AddSingleton<IValidator<UpdateProduct.Command>, UpdateProduct.Validator>();

        services.AddSingleton<CreateProduct.Handler>();
        services.AddSingleton<UpdateProduct.Handler>();
        services.AddSingleton<DeleteProduct.Handler>();
        services.AddSingleton<ListProducts.Handler>();
        services.AddSingleton<GetProductPhoto.Handler>();
        services.AddSingleton<SetProductPhoto.Handler>();
        services.AddSingleton<ProductPhotoPanelFactory>();

        services.AddSingleton<IValidator<CreateProductCategory.Command>, CreateProductCategory.Validator>();
        services.AddSingleton<IValidator<UpdateProductCategory.Command>, UpdateProductCategory.Validator>();

        services.AddSingleton<CreateProductCategory.Handler>();
        services.AddSingleton<GetProductCategory.Handler>();
        services.AddSingleton<ListProductCategories.Handler>();
        services.AddSingleton<UpdateProductCategory.Handler>();
        services.AddSingleton<DeleteProductCategory.Handler>();

        services.AddSingleton<IValidator<CreateSupplier.Command>, CreateSupplier.Validator>();
        services.AddSingleton<IValidator<UpdateSupplier.Command>, UpdateSupplier.Validator>();
        services.AddSingleton<IValidator<SetSupplierPrice.Command>, SetSupplierPrice.Validator>();

        services.AddSingleton<CreateSupplier.Handler>();
        services.AddSingleton<UpdateSupplier.Handler>();
        services.AddSingleton<DeleteSupplier.Handler>();
        services.AddSingleton<ListSuppliers.Handler>();
        services.AddSingleton<SetSupplierPrice.Handler>();
        services.AddSingleton<DeleteSupplierPrice.Handler>();
        services.AddSingleton<ListSupplierPrices.Handler>();
        services.AddSingleton<SupplierPricesViewModelFactory>();

        services.AddSingleton<IValidator<CreateStorageLocation.Command>, CreateStorageLocation.Validator>();
        services.AddSingleton<IValidator<UpdateStorageLocation.Command>, UpdateStorageLocation.Validator>();
        services.AddSingleton<IValidator<ReceiveStock.Command>, ReceiveStock.Validator>();
        services.AddSingleton<IValidator<IssueStock.Command>, IssueStock.Validator>();
        services.AddSingleton<IValidator<TransferStock.Command>, TransferStock.Validator>();
        services.AddSingleton<IValidator<CreateInventoryCount.Command>, CreateInventoryCount.Validator>();

        services.AddSingleton<CreateStorageLocation.Handler>();
        services.AddSingleton<UpdateStorageLocation.Handler>();
        services.AddSingleton<DeleteStorageLocation.Handler>();
        services.AddSingleton<ListStorageLocations.Handler>();
        services.AddSingleton<ReceiveStock.Handler>();
        services.AddSingleton<IssueStock.Handler>();
        services.AddSingleton<TransferStock.Handler>();
        services.AddSingleton<ListStockOverview.Handler>();
        services.AddSingleton<GetProductStock.Handler>();
        services.AddSingleton<ListStockMovements.Handler>();
        services.AddSingleton<CreateInventoryCount.Handler>();
        services.AddSingleton<ListInventoryCounts.Handler>();
        services.AddSingleton<GetInventoryCount.Handler>();
        services.AddSingleton<SaveInventoryCountLines.Handler>();
        services.AddSingleton<PostInventoryCount.Handler>();
        services.AddSingleton<DeleteInventoryCount.Handler>();

        services.AddTransient<StockViewModel>();
        services.AddTransient<StockMovementsViewModel>();
        services.AddTransient<InventoryCountsViewModel>();
        services.AddTransient<StorageLocationsViewModel>();
        services.AddTransient<ProductsViewModel>();
        services.AddTransient<SuppliersViewModel>();
        services.AddTransient<ProductCategoriesViewModel>();
        services.AddSingleton<IModulePage, ProductsPage>();
        services.AddSingleton<IModulePage, ProductCategoriesPage>();
        services.AddSingleton<IModulePage, SuppliersPage>();
        services.AddSingleton<IModulePage, StockPage>();
        services.AddSingleton<IModulePage, StockMovementsPage>();
        services.AddSingleton<IModulePage, InventoryCountsPage>();
        services.AddSingleton<IModulePage, StorageLocationsPage>();

        services.AddSingleton(new ModuleInfo("products", "Products_Title"));
        services.AddSingleton<IStartupTask, MigrateProductsDatabaseTask>();

        return services;
    }
}
