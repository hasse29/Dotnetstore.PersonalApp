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
using Dotnetstore.PersonalApp.Modules.Production.Persistence;
using Dotnetstore.PersonalApp.Shared.Events;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Dotnetstore.PersonalApp.Modules.Production.Tests;

/// <summary>
/// Real SQLite (in-memory, kept alive by one open connection) with the module's handlers resolved from a service provider.
/// </summary>
internal sealed class ProductionFixture : IDisposable
{
    private readonly SqliteConnection _connection = new("Data Source=:memory:");
    private int _counter;

    public ProductionFixture()
    {
        _connection.Open();
        var options = new DbContextOptionsBuilder<ProductionDbContext>().UseSqlite(_connection).Options;

        var services = new ServiceCollection();
        services.AddSingleton(TimeProvider.System);
        Dotnetstore.PersonalApp.Shared.Localization.Localizer.Instance.AddResources(Dotnetstore.PersonalApp.Modules.Production.Resources.ProductionStrings.ResourceManager);
        services.AddSingleton<Dotnetstore.PersonalApp.Shared.Localization.ILocalizationService>(Dotnetstore.PersonalApp.Shared.Localization.Localizer.Instance);
        services.AddSingleton<RecordingEventPublisher>();
        services.AddSingleton<IEventPublisher>(sp => sp.GetRequiredService<RecordingEventPublisher>());
        services.AddSingleton<IDbContextFactory<ProductionDbContext>>(new TestDbFactory(options));
        ProductionModule.AddFeatures(services);
        Services = services.BuildServiceProvider();

        using var db = Services.GetRequiredService<IDbContextFactory<ProductionDbContext>>().CreateDbContext();
        db.Database.EnsureCreated();
    }

    public ServiceProvider Services { get; }

    public Task<CreateUnitMeasure.Command> UnitMeasureCommandAsync(CancellationToken cancellationToken)
    {
        var n = ++_counter;
        return Task.FromResult(new CreateUnitMeasure.Command(
            $"Unit{n}",
            $"Unit{n}"));
    }

    public async Task<UnitMeasureDto> MakeUnitMeasureAsync(CancellationToken cancellationToken) =>
        (await Services.GetRequiredService<CreateUnitMeasure.Handler>()
            .HandleAsync(await UnitMeasureCommandAsync(cancellationToken), cancellationToken)).Value;

    public Task<CreateProductCategory.Command> ProductCategoryCommandAsync(CancellationToken cancellationToken)
    {
        var n = ++_counter;
        return Task.FromResult(new CreateProductCategory.Command(
            $"Product{n}"));
    }

    public async Task<ProductCategoryDto> MakeProductCategoryAsync(CancellationToken cancellationToken) =>
        (await Services.GetRequiredService<CreateProductCategory.Handler>()
            .HandleAsync(await ProductCategoryCommandAsync(cancellationToken), cancellationToken)).Value;

    public async Task<CreateProductSubcategory.Command> ProductSubcategoryCommandAsync(CancellationToken cancellationToken)
    {
        var n = ++_counter;
        return new CreateProductSubcategory.Command(
            (await MakeProductCategoryAsync(cancellationToken)).Id,
            $"Product{n}");
    }

    public async Task<ProductSubcategoryDto> MakeProductSubcategoryAsync(CancellationToken cancellationToken) =>
        (await Services.GetRequiredService<CreateProductSubcategory.Handler>()
            .HandleAsync(await ProductSubcategoryCommandAsync(cancellationToken), cancellationToken)).Value;

    public Task<CreateProductModel.Command> ProductModelCommandAsync(CancellationToken cancellationToken)
    {
        var n = ++_counter;
        return Task.FromResult(new CreateProductModel.Command(
            $"Product{n}",
            $"Product{n}",
            $"Product{n}"));
    }

    public async Task<ProductModelDto> MakeProductModelAsync(CancellationToken cancellationToken) =>
        (await Services.GetRequiredService<CreateProductModel.Handler>()
            .HandleAsync(await ProductModelCommandAsync(cancellationToken), cancellationToken)).Value;

    public Task<CreateScrapReason.Command> ScrapReasonCommandAsync(CancellationToken cancellationToken)
    {
        var n = ++_counter;
        return Task.FromResult(new CreateScrapReason.Command(
            $"Scrap{n}"));
    }

    public async Task<ScrapReasonDto> MakeScrapReasonAsync(CancellationToken cancellationToken) =>
        (await Services.GetRequiredService<CreateScrapReason.Handler>()
            .HandleAsync(await ScrapReasonCommandAsync(cancellationToken), cancellationToken)).Value;

    public Task<CreateLocation.Command> LocationCommandAsync(CancellationToken cancellationToken)
    {
        var n = ++_counter;
        return Task.FromResult(new CreateLocation.Command(
            $"Location{n}",
            1m,
            1m));
    }

    public async Task<LocationDto> MakeLocationAsync(CancellationToken cancellationToken) =>
        (await Services.GetRequiredService<CreateLocation.Handler>()
            .HandleAsync(await LocationCommandAsync(cancellationToken), cancellationToken)).Value;

    public Task<CreateProduct.Command> ProductCommandAsync(CancellationToken cancellationToken)
    {
        var n = ++_counter;
        return Task.FromResult(new CreateProduct.Command(
            $"Product{n}",
            $"Product{n}",
            false,
            false,
            $"Product{n}",
            1,
            1,
            1m,
            1m,
            $"S{n}",
            null,
            1m,
            null,
            1,
            "N",
            "N",
            "N",
            null,
            null,
            new DateOnly(2026, 1, 1),
            new DateOnly(2026, 1, 1),
            new DateOnly(2026, 1, 1)));
    }

    public async Task<ProductDto> MakeProductAsync(CancellationToken cancellationToken) =>
        (await Services.GetRequiredService<CreateProduct.Handler>()
            .HandleAsync(await ProductCommandAsync(cancellationToken), cancellationToken)).Value;

    public async Task<CreateProductCostHistory.Command> ProductCostHistoryCommandAsync(CancellationToken cancellationToken)
    {
        var n = ++_counter;
        return new CreateProductCostHistory.Command(
            (await MakeProductAsync(cancellationToken)).Id,
            new DateOnly(2026, 1, 1),
            new DateOnly(2026, 1, 1),
            1m);
    }

    public async Task<ProductCostHistoryDto> MakeProductCostHistoryAsync(CancellationToken cancellationToken) =>
        (await Services.GetRequiredService<CreateProductCostHistory.Handler>()
            .HandleAsync(await ProductCostHistoryCommandAsync(cancellationToken), cancellationToken)).Value;

    public async Task<CreateProductListPriceHistory.Command> ProductListPriceHistoryCommandAsync(CancellationToken cancellationToken)
    {
        var n = ++_counter;
        return new CreateProductListPriceHistory.Command(
            (await MakeProductAsync(cancellationToken)).Id,
            new DateOnly(2026, 1, 1),
            new DateOnly(2026, 1, 1),
            1m);
    }

    public async Task<ProductListPriceHistoryDto> MakeProductListPriceHistoryAsync(CancellationToken cancellationToken) =>
        (await Services.GetRequiredService<CreateProductListPriceHistory.Handler>()
            .HandleAsync(await ProductListPriceHistoryCommandAsync(cancellationToken), cancellationToken)).Value;

    public async Task<CreateProductInventory.Command> ProductInventoryCommandAsync(CancellationToken cancellationToken)
    {
        var n = ++_counter;
        return new CreateProductInventory.Command(
            (await MakeProductAsync(cancellationToken)).Id,
            (await MakeLocationAsync(cancellationToken)).Id,
            $"Product{n}",
            1,
            1);
    }

    public async Task<ProductInventoryDto> MakeProductInventoryAsync(CancellationToken cancellationToken) =>
        (await Services.GetRequiredService<CreateProductInventory.Handler>()
            .HandleAsync(await ProductInventoryCommandAsync(cancellationToken), cancellationToken)).Value;

    public async Task<CreateProductReview.Command> ProductReviewCommandAsync(CancellationToken cancellationToken)
    {
        var n = ++_counter;
        return new CreateProductReview.Command(
            (await MakeProductAsync(cancellationToken)).Id,
            $"Product{n}",
            new DateOnly(2026, 1, 1),
            $"Product{n}",
            1,
            $"Product{n}");
    }

    public async Task<ProductReviewDto> MakeProductReviewAsync(CancellationToken cancellationToken) =>
        (await Services.GetRequiredService<CreateProductReview.Handler>()
            .HandleAsync(await ProductReviewCommandAsync(cancellationToken), cancellationToken)).Value;

    public async Task<CreateBillOfMaterial.Command> BillOfMaterialCommandAsync(CancellationToken cancellationToken)
    {
        var n = ++_counter;
        return new CreateBillOfMaterial.Command(
            null,
            (await MakeProductAsync(cancellationToken)).Id,
            new DateOnly(2026, 1, 1),
            new DateOnly(2026, 1, 1),
            (await MakeUnitMeasureAsync(cancellationToken)).Id,
            1,
            1m);
    }

    public async Task<BillOfMaterialDto> MakeBillOfMaterialAsync(CancellationToken cancellationToken) =>
        (await Services.GetRequiredService<CreateBillOfMaterial.Handler>()
            .HandleAsync(await BillOfMaterialCommandAsync(cancellationToken), cancellationToken)).Value;

    public async Task<CreateWorkOrder.Command> WorkOrderCommandAsync(CancellationToken cancellationToken)
    {
        var n = ++_counter;
        return new CreateWorkOrder.Command(
            $"Work{n}",
            (await MakeProductAsync(cancellationToken)).Id,
            1,
            1,
            new DateOnly(2026, 1, 1),
            new DateOnly(2026, 1, 1),
            new DateOnly(2026, 1, 1),
            null);
    }

    public async Task<WorkOrderDto> MakeWorkOrderAsync(CancellationToken cancellationToken) =>
        (await Services.GetRequiredService<CreateWorkOrder.Handler>()
            .HandleAsync(await WorkOrderCommandAsync(cancellationToken), cancellationToken)).Value;

    public async Task<CreateWorkOrderRouting.Command> WorkOrderRoutingCommandAsync(CancellationToken cancellationToken)
    {
        var n = ++_counter;
        return new CreateWorkOrderRouting.Command(
            (await MakeWorkOrderAsync(cancellationToken)).Id,
            1,
            (await MakeLocationAsync(cancellationToken)).Id,
            new DateOnly(2026, 1, 1),
            new DateOnly(2026, 1, 1),
            new DateOnly(2026, 1, 1),
            new DateOnly(2026, 1, 1),
            1m,
            1m,
            1m);
    }

    public async Task<WorkOrderRoutingDto> MakeWorkOrderRoutingAsync(CancellationToken cancellationToken) =>
        (await Services.GetRequiredService<CreateWorkOrderRouting.Handler>()
            .HandleAsync(await WorkOrderRoutingCommandAsync(cancellationToken), cancellationToken)).Value;

    public async Task<CreateTransactionHistory.Command> TransactionHistoryCommandAsync(CancellationToken cancellationToken)
    {
        var n = ++_counter;
        return new CreateTransactionHistory.Command(
            (await MakeProductAsync(cancellationToken)).Id,
            $"Transaction{n}",
            new DateOnly(2026, 1, 1),
            "W",
            1,
            1m);
    }

    public async Task<TransactionHistoryDto> MakeTransactionHistoryAsync(CancellationToken cancellationToken) =>
        (await Services.GetRequiredService<CreateTransactionHistory.Handler>()
            .HandleAsync(await TransactionHistoryCommandAsync(cancellationToken), cancellationToken)).Value;

    public void Dispose()
    {
        Services.Dispose();
        _connection.Dispose();
    }

    private sealed class TestDbFactory(DbContextOptions<ProductionDbContext> options) : IDbContextFactory<ProductionDbContext>
    {
        public ProductionDbContext CreateDbContext() => new(options);
    }
}