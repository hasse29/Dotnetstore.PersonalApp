using Dotnetstore.PersonalApp.Modules.Production.Features.BillOfMaterials;
using Dotnetstore.PersonalApp.Modules.Production.Features.Locations;
using Dotnetstore.PersonalApp.Modules.Production.Features.ProductCategories;
using Dotnetstore.PersonalApp.Modules.Production.Features.ProductCostHistories;
using Dotnetstore.PersonalApp.Modules.Production.Features.ProductInventories;
using Dotnetstore.PersonalApp.Modules.Production.Features.ProductListPriceHistories;
using Dotnetstore.PersonalApp.Modules.Production.Features.ProductModels;
using Dotnetstore.PersonalApp.Modules.Production.Features.ProductReviews;
using Dotnetstore.PersonalApp.Modules.Production.Features.ProductSubcategories;
using Dotnetstore.PersonalApp.Modules.Production.Features.Products;
using Dotnetstore.PersonalApp.Modules.Production.Features.ScrapReasons;
using Dotnetstore.PersonalApp.Modules.Production.Features.TransactionHistories;
using Dotnetstore.PersonalApp.Modules.Production.Features.UnitMeasures;
using Dotnetstore.PersonalApp.Modules.Production.Features.WorkOrderRoutings;
using Dotnetstore.PersonalApp.Modules.Production.Features.WorkOrders;
using Dotnetstore.PersonalApp.Modules.Production.Features;
using Dotnetstore.PersonalApp.Modules.Production.Persistence;
using Dotnetstore.PersonalApp.Modules.Production.Resources;
using Dotnetstore.PersonalApp.Modules.Production.Startup;
using Dotnetstore.PersonalApp.Modules.Production.Dashboard;
using Dotnetstore.PersonalApp.Shared.Dashboard;
using Dotnetstore.PersonalApp.Shared.Products;
using Dotnetstore.PersonalApp.Shared.Contracts.Events;
using Dotnetstore.PersonalApp.Shared.Events;
using Dotnetstore.PersonalApp.Shared.Localization;
using Dotnetstore.PersonalApp.Shared.Shell;
using Dotnetstore.PersonalApp.Shared.Startup;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Dotnetstore.PersonalApp.Modules.Production;

public static class ProductionModule
{
    public static IServiceCollection AddProductionModule(this IServiceCollection services, string databasePath)
    {
        Localizer.Instance.AddResources(ProductionStrings.ResourceManager);

        services.AddDbContextFactory<ProductionDbContext>(options => options.UseSqlite($"Data Source={databasePath}"));

        AddFeatures(services);
        services.AddSingleton<IUnitMeasureLookup, UnitMeasureLookup>();
        services.AddSingleton<IProductLookup, ProductLookup>();

        services.AddSingleton(new ModuleInfo(ProductionGroup.ModuleKey, "Production_Title"));
        services.AddSingleton<IModulePage>(sp => new EntityPage<UnitMeasuresViewModel>(sp, "UnitMeasures_Title"));
        services.AddSingleton<IModulePage>(sp => new EntityPage<ProductCategoriesViewModel>(sp, "ProductCategories_Title"));
        services.AddSingleton<IModulePage>(sp => new EntityPage<ProductSubcategoriesViewModel>(sp, "ProductSubcategories_Title"));
        services.AddSingleton<IModulePage>(sp => new EntityPage<ProductModelsViewModel>(sp, "ProductModels_Title"));
        services.AddSingleton<IModulePage>(sp => new EntityPage<ScrapReasonsViewModel>(sp, "ScrapReasons_Title"));
        services.AddSingleton<IModulePage>(sp => new EntityPage<LocationsViewModel>(sp, "Locations_Title"));
        services.AddSingleton<IModulePage>(sp => new EntityPage<ProductsViewModel>(sp, "Products_Title"));
        services.AddSingleton<IModulePage>(sp => new EntityPage<ProductCostHistoriesViewModel>(sp, "ProductCostHistories_Title"));
        services.AddSingleton<IModulePage>(sp => new EntityPage<ProductListPriceHistoriesViewModel>(sp, "ProductListPriceHistories_Title"));
        services.AddSingleton<IModulePage>(sp => new EntityPage<ProductInventoriesViewModel>(sp, "ProductInventories_Title"));
        services.AddSingleton<IModulePage>(sp => new EntityPage<ProductReviewsViewModel>(sp, "ProductReviews_Title"));
        services.AddSingleton<IModulePage>(sp => new EntityPage<BillOfMaterialsViewModel>(sp, "BillOfMaterials_Title"));
        services.AddSingleton<IModulePage>(sp => new EntityPage<WorkOrdersViewModel>(sp, "WorkOrders_Title"));
        services.AddSingleton<IModulePage>(sp => new EntityPage<WorkOrderRoutingsViewModel>(sp, "WorkOrderRoutings_Title"));
        services.AddSingleton<IModulePage>(sp => new EntityPage<TransactionHistoriesViewModel>(sp, "TransactionHistories_Title"));
        services.AddSingleton<IStartupTask, MigrateProductionDatabaseTask>();

        return services;
    }

    /// <summary>Handlers, validators and view models; separate so tests can build them without a database file.</summary>
    internal static void AddFeatures(IServiceCollection services)
    {
        services.AddSingleton<IValidator<CreateUnitMeasure.Command>, CreateUnitMeasure.Validator>();
        services.AddSingleton<IValidator<UpdateUnitMeasure.Command>, UpdateUnitMeasure.Validator>();
        services.AddSingleton<CreateUnitMeasure.Handler>();
        services.AddSingleton<UpdateUnitMeasure.Handler>();
        services.AddSingleton<DeleteUnitMeasure.Handler>();
        services.AddSingleton<ListUnitMeasures.Handler>();
        services.AddSingleton<ListUnitMeasureOptions.Handler>();
        services.AddTransient<UnitMeasuresViewModel>();

        services.AddSingleton<IValidator<CreateProductCategory.Command>, CreateProductCategory.Validator>();
        services.AddSingleton<IValidator<UpdateProductCategory.Command>, UpdateProductCategory.Validator>();
        services.AddSingleton<CreateProductCategory.Handler>();
        services.AddSingleton<UpdateProductCategory.Handler>();
        services.AddSingleton<DeleteProductCategory.Handler>();
        services.AddSingleton<ListProductCategories.Handler>();
        services.AddSingleton<ListProductCategoryOptions.Handler>();
        services.AddTransient<ProductCategoriesViewModel>();

        services.AddSingleton<IValidator<CreateProductSubcategory.Command>, CreateProductSubcategory.Validator>();
        services.AddSingleton<IValidator<UpdateProductSubcategory.Command>, UpdateProductSubcategory.Validator>();
        services.AddSingleton<CreateProductSubcategory.Handler>();
        services.AddSingleton<UpdateProductSubcategory.Handler>();
        services.AddSingleton<DeleteProductSubcategory.Handler>();
        services.AddSingleton<ListProductSubcategories.Handler>();
        services.AddSingleton<ListProductSubcategoryOptions.Handler>();
        services.AddTransient<ProductSubcategoriesViewModel>();

        services.AddSingleton<IValidator<CreateProductModel.Command>, CreateProductModel.Validator>();
        services.AddSingleton<IValidator<UpdateProductModel.Command>, UpdateProductModel.Validator>();
        services.AddSingleton<CreateProductModel.Handler>();
        services.AddSingleton<UpdateProductModel.Handler>();
        services.AddSingleton<DeleteProductModel.Handler>();
        services.AddSingleton<ListProductModels.Handler>();
        services.AddSingleton<ListProductModelOptions.Handler>();
        services.AddTransient<ProductModelsViewModel>();

        services.AddSingleton<IValidator<CreateScrapReason.Command>, CreateScrapReason.Validator>();
        services.AddSingleton<IValidator<UpdateScrapReason.Command>, UpdateScrapReason.Validator>();
        services.AddSingleton<CreateScrapReason.Handler>();
        services.AddSingleton<UpdateScrapReason.Handler>();
        services.AddSingleton<DeleteScrapReason.Handler>();
        services.AddSingleton<ListScrapReasons.Handler>();
        services.AddSingleton<ListScrapReasonOptions.Handler>();
        services.AddTransient<ScrapReasonsViewModel>();

        services.AddSingleton<IValidator<CreateLocation.Command>, CreateLocation.Validator>();
        services.AddSingleton<IValidator<UpdateLocation.Command>, UpdateLocation.Validator>();
        services.AddSingleton<CreateLocation.Handler>();
        services.AddSingleton<UpdateLocation.Handler>();
        services.AddSingleton<DeleteLocation.Handler>();
        services.AddSingleton<ListLocations.Handler>();
        services.AddSingleton<ListLocationOptions.Handler>();
        services.AddTransient<LocationsViewModel>();

        services.AddSingleton<IValidator<CreateProduct.Command>, CreateProduct.Validator>();
        services.AddSingleton<IValidator<UpdateProduct.Command>, UpdateProduct.Validator>();
        services.AddSingleton<CreateProduct.Handler>();
        services.AddSingleton<UpdateProduct.Handler>();
        services.AddSingleton<DeleteProduct.Handler>();
        services.AddSingleton<ListProducts.Handler>();
        services.AddSingleton<ListProductOptions.Handler>();
        services.AddTransient<ProductsViewModel>();
        services.AddTransient<IDetailFactory<ProductDto>, DetailFactory<ProductDto, ProductDetailViewModel>>();

        services.AddSingleton<IValidator<CreateProductCostHistory.Command>, CreateProductCostHistory.Validator>();
        services.AddSingleton<IValidator<UpdateProductCostHistory.Command>, UpdateProductCostHistory.Validator>();
        services.AddSingleton<CreateProductCostHistory.Handler>();
        services.AddSingleton<UpdateProductCostHistory.Handler>();
        services.AddSingleton<DeleteProductCostHistory.Handler>();
        services.AddSingleton<ListProductCostHistories.Handler>();
        services.AddTransient<ProductCostHistoriesViewModel>();

        services.AddSingleton<IValidator<CreateProductListPriceHistory.Command>, CreateProductListPriceHistory.Validator>();
        services.AddSingleton<IValidator<UpdateProductListPriceHistory.Command>, UpdateProductListPriceHistory.Validator>();
        services.AddSingleton<CreateProductListPriceHistory.Handler>();
        services.AddSingleton<UpdateProductListPriceHistory.Handler>();
        services.AddSingleton<DeleteProductListPriceHistory.Handler>();
        services.AddSingleton<ListProductListPriceHistories.Handler>();
        services.AddTransient<ProductListPriceHistoriesViewModel>();

        services.AddSingleton<IValidator<CreateProductInventory.Command>, CreateProductInventory.Validator>();
        services.AddSingleton<IValidator<UpdateProductInventory.Command>, UpdateProductInventory.Validator>();
        services.AddSingleton<CreateProductInventory.Handler>();
        services.AddSingleton<UpdateProductInventory.Handler>();
        services.AddSingleton<DeleteProductInventory.Handler>();
        services.AddSingleton<ListProductInventories.Handler>();
        services.AddTransient<ProductInventoriesViewModel>();

        services.AddSingleton<IValidator<CreateProductReview.Command>, CreateProductReview.Validator>();
        services.AddSingleton<IValidator<UpdateProductReview.Command>, UpdateProductReview.Validator>();
        services.AddSingleton<CreateProductReview.Handler>();
        services.AddSingleton<UpdateProductReview.Handler>();
        services.AddSingleton<DeleteProductReview.Handler>();
        services.AddSingleton<ListProductReviews.Handler>();
        services.AddTransient<ProductReviewsViewModel>();

        services.AddSingleton<IValidator<CreateBillOfMaterial.Command>, CreateBillOfMaterial.Validator>();
        services.AddSingleton<IValidator<UpdateBillOfMaterial.Command>, UpdateBillOfMaterial.Validator>();
        services.AddSingleton<CreateBillOfMaterial.Handler>();
        services.AddSingleton<UpdateBillOfMaterial.Handler>();
        services.AddSingleton<DeleteBillOfMaterial.Handler>();
        services.AddSingleton<ListBillOfMaterials.Handler>();
        services.AddTransient<BillOfMaterialsViewModel>();

        services.AddSingleton<IValidator<CreateWorkOrder.Command>, CreateWorkOrder.Validator>();
        services.AddSingleton<IValidator<UpdateWorkOrder.Command>, UpdateWorkOrder.Validator>();
        services.AddSingleton<CreateWorkOrder.Handler>();
        services.AddSingleton<UpdateWorkOrder.Handler>();
        services.AddSingleton<DeleteWorkOrder.Handler>();
        services.AddSingleton<ListWorkOrders.Handler>();
        services.AddSingleton<ListWorkOrderOptions.Handler>();
        services.AddTransient<WorkOrdersViewModel>();

        services.AddSingleton<IValidator<CreateWorkOrderRouting.Command>, CreateWorkOrderRouting.Validator>();
        services.AddSingleton<IValidator<UpdateWorkOrderRouting.Command>, UpdateWorkOrderRouting.Validator>();
        services.AddSingleton<CreateWorkOrderRouting.Handler>();
        services.AddSingleton<UpdateWorkOrderRouting.Handler>();
        services.AddSingleton<DeleteWorkOrderRouting.Handler>();
        services.AddSingleton<ListWorkOrderRoutings.Handler>();
        services.AddTransient<WorkOrderRoutingsViewModel>();

        services.AddSingleton<IValidator<CreateTransactionHistory.Command>, CreateTransactionHistory.Validator>();
        services.AddSingleton<IValidator<UpdateTransactionHistory.Command>, UpdateTransactionHistory.Validator>();
        services.AddSingleton<CreateTransactionHistory.Handler>();
        services.AddSingleton<UpdateTransactionHistory.Handler>();
        services.AddSingleton<DeleteTransactionHistory.Handler>();
        services.AddSingleton<ListTransactionHistories.Handler>();
        services.AddTransient<TransactionHistoriesViewModel>();

        services.AddSingleton<IDashboardWidget, ProductionDashboardWidget>();
    }
}