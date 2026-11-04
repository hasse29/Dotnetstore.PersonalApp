using Dotnetstore.PersonalApp.Modules.Inventory.Consumers;
using Dotnetstore.PersonalApp.Modules.Inventory.Features.InventoryCountLines;
using Dotnetstore.PersonalApp.Modules.Inventory.Features.InventoryCounts;
using Dotnetstore.PersonalApp.Modules.Inventory.Features.StockLevels;
using Dotnetstore.PersonalApp.Modules.Inventory.Features.StockMovements;
using Dotnetstore.PersonalApp.Modules.Inventory.Features.StorageLocations;
using Dotnetstore.PersonalApp.Modules.Inventory.Features.Warehouses;
using Dotnetstore.PersonalApp.Modules.Inventory.Features;
using Dotnetstore.PersonalApp.Modules.Inventory.Persistence;
using Dotnetstore.PersonalApp.Modules.Inventory.Resources;
using Dotnetstore.PersonalApp.Modules.Inventory.Startup;
using Dotnetstore.PersonalApp.Modules.Inventory.Dashboard;
using Dotnetstore.PersonalApp.Modules.Inventory.Features.Receiving;
using Dotnetstore.PersonalApp.Modules.Inventory.Features.StockOverview;
using Dotnetstore.PersonalApp.Modules.Inventory.Features.Transfers;
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

namespace Dotnetstore.PersonalApp.Modules.Inventory;

public static class InventoryModule
{
    public static IServiceCollection AddInventoryModule(this IServiceCollection services, string databasePath)
    {
        Localizer.Instance.AddResources(InventoryStrings.ResourceManager);

        services.AddDbContextFactory<InventoryDbContext>(options => options.UseSqlite($"Data Source={databasePath}"));

        AddFeatures(services);


        services.AddSingleton(new ModuleInfo(InventoryGroup.ModuleKey, "Inventory_Title"));
        services.AddSingleton<IModulePage>(sp => new EntityPage<WarehousesViewModel>(sp, "Warehouses_Title"));
        services.AddSingleton<IModulePage>(sp => new EntityPage<StorageLocationsViewModel>(sp, "StorageLocations_Title"));
        services.AddSingleton<IModulePage>(sp => new EntityPage<StockLevelsViewModel>(sp, "StockLevels_Title"));
        services.AddSingleton<IModulePage>(sp => new EntityPage<StockMovementsViewModel>(sp, "StockMovements_Title"));
        services.AddSingleton<IModulePage>(sp => new EntityPage<InventoryCountsViewModel>(sp, "InventoryCounts_Title"));
        services.AddSingleton<IModulePage>(sp => new EntityPage<InventoryCountLinesViewModel>(sp, "InventoryCountLines_Title"));
        services.AddSingleton<IModulePage>(sp => new EntityPage<StockOverviewViewModel>(sp, "StockOverview_Title"));
        services.AddSingleton<IModulePage>(sp => new EntityPage<GoodsReceiptViewModel>(sp, "GoodsReceipt_Title"));
        services.AddSingleton<IModulePage>(sp => new EntityPage<GoodsIssueViewModel>(sp, "GoodsIssue_Title"));
        services.AddSingleton<IModulePage>(sp => new EntityPage<StockTransferViewModel>(sp, "StockTransfer_Title"));
        services.AddSingleton<IStartupTask, MigrateInventoryDatabaseTask>();

        return services;
    }

    /// <summary>Handlers, validators and view models; separate so tests can build them without a database file.</summary>
    internal static void AddFeatures(IServiceCollection services)
    {
        services.AddSingleton<IValidator<CreateWarehouse.Command>, CreateWarehouse.Validator>();
        services.AddSingleton<IValidator<UpdateWarehouse.Command>, UpdateWarehouse.Validator>();
        services.AddSingleton<CreateWarehouse.Handler>();
        services.AddSingleton<UpdateWarehouse.Handler>();
        services.AddSingleton<DeleteWarehouse.Handler>();
        services.AddSingleton<ListWarehouses.Handler>();
        services.AddSingleton<ListWarehouseOptions.Handler>();
        services.AddTransient<WarehousesViewModel>();
        services.AddTransient<IDetailFactory<WarehouseDto>, DetailFactory<WarehouseDto, WarehouseDetailViewModel>>();

        services.AddSingleton<IValidator<CreateStorageLocation.Command>, CreateStorageLocation.Validator>();
        services.AddSingleton<IValidator<UpdateStorageLocation.Command>, UpdateStorageLocation.Validator>();
        services.AddSingleton<CreateStorageLocation.Handler>();
        services.AddSingleton<UpdateStorageLocation.Handler>();
        services.AddSingleton<DeleteStorageLocation.Handler>();
        services.AddSingleton<ListStorageLocations.Handler>();
        services.AddSingleton<ListStorageLocationOptions.Handler>();
        services.AddTransient<StorageLocationsViewModel>();
        services.AddTransient<IDetailFactory<StorageLocationDto>, DetailFactory<StorageLocationDto, StorageLocationDetailViewModel>>();

        services.AddSingleton<IValidator<CreateStockLevel.Command>, CreateStockLevel.Validator>();
        services.AddSingleton<IValidator<UpdateStockLevel.Command>, UpdateStockLevel.Validator>();
        services.AddSingleton<CreateStockLevel.Handler>();
        services.AddSingleton<UpdateStockLevel.Handler>();
        services.AddSingleton<DeleteStockLevel.Handler>();
        services.AddSingleton<ListStockLevels.Handler>();
        services.AddTransient<StockLevelsViewModel>();

        services.AddSingleton<IValidator<CreateStockMovement.Command>, CreateStockMovement.Validator>();
        services.AddSingleton<IValidator<UpdateStockMovement.Command>, UpdateStockMovement.Validator>();
        services.AddSingleton<CreateStockMovement.Handler>();
        services.AddSingleton<UpdateStockMovement.Handler>();
        services.AddSingleton<DeleteStockMovement.Handler>();
        services.AddSingleton<ListStockMovements.Handler>();
        services.AddTransient<StockMovementsViewModel>();

        services.AddSingleton<IValidator<CreateInventoryCount.Command>, CreateInventoryCount.Validator>();
        services.AddSingleton<IValidator<UpdateInventoryCount.Command>, UpdateInventoryCount.Validator>();
        services.AddSingleton<CreateInventoryCount.Handler>();
        services.AddSingleton<UpdateInventoryCount.Handler>();
        services.AddSingleton<DeleteInventoryCount.Handler>();
        services.AddSingleton<ListInventoryCounts.Handler>();
        services.AddSingleton<ListInventoryCountOptions.Handler>();
        services.AddTransient<InventoryCountsViewModel>();
        services.AddTransient<IDetailFactory<InventoryCountDto>, DetailFactory<InventoryCountDto, InventoryCountScreenViewModel>>();

        services.AddSingleton<IValidator<CreateInventoryCountLine.Command>, CreateInventoryCountLine.Validator>();
        services.AddSingleton<IValidator<UpdateInventoryCountLine.Command>, UpdateInventoryCountLine.Validator>();
        services.AddSingleton<CreateInventoryCountLine.Handler>();
        services.AddSingleton<UpdateInventoryCountLine.Handler>();
        services.AddSingleton<DeleteInventoryCountLine.Handler>();
        services.AddSingleton<ListInventoryCountLines.Handler>();
        services.AddTransient<InventoryCountLinesViewModel>();

        services.AddSingleton<ExternalReferences>();
        services.AddSingleton<IIntegrationEventConsumer<ProductDeleted>, ProductDeletedConsumer>();
        services.AddSingleton<ListStockOverview.Handler>();
        services.AddSingleton<OrderMovements.Handler>();
        services.AddSingleton<TransferStock.Handler>();
        services.AddSingleton<FillInventoryCount.Handler>();
        services.AddSingleton<PostInventoryCount.Handler>();
        services.AddTransient<StockOverviewViewModel>();
        services.AddTransient<GoodsReceiptViewModel>();
        services.AddTransient<GoodsIssueViewModel>();
        services.AddTransient<StockTransferViewModel>();
        services.AddSingleton<IDashboardWidget, InventoryDashboardWidget>();
    }
}