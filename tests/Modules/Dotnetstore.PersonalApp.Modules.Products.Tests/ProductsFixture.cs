using Dotnetstore.PersonalApp.Modules.Products.Domain;
using Dotnetstore.PersonalApp.Modules.Products.Features.ProductCategories;
using Dotnetstore.PersonalApp.Modules.Products.Features.InventoryCounts;
using Dotnetstore.PersonalApp.Modules.Products.Features.Products;
using Dotnetstore.PersonalApp.Modules.Products.Features.Stock;
using Dotnetstore.PersonalApp.Modules.Products.Features.StorageLocations;
using Dotnetstore.PersonalApp.Modules.Products.Features.SupplierPrices;
using Dotnetstore.PersonalApp.Modules.Products.Features.Suppliers;
using Dotnetstore.PersonalApp.Modules.Products.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Dotnetstore.PersonalApp.Modules.Products.Tests;

/// <summary>Real SQLite (in-memory, kept alive by one open connection) with the module's handlers.</summary>
internal sealed class ProductsFixture : IDisposable
{
    private readonly SqliteConnection _connection = new("Data Source=:memory:");

    public ProductsFixture()
    {
        _connection.Open();
        DbFactory = new TestDbFactory(new DbContextOptionsBuilder<ProductsDbContext>().UseSqlite(_connection).Options);
        using var db = DbFactory.CreateDbContext();
        db.Database.EnsureCreated();

        Create = new CreateProduct.Handler(DbFactory, new CreateProduct.Validator(), Clock);
        Update = new UpdateProduct.Handler(DbFactory, new UpdateProduct.Validator());
        Delete = new DeleteProduct.Handler(DbFactory);
        List = new ListProducts.Handler(DbFactory);
        CreateCategory = new CreateProductCategory.Handler(DbFactory, new CreateProductCategory.Validator(), Clock);
        DeleteCategory = new DeleteProductCategory.Handler(DbFactory);
        CreateSupplier = new CreateSupplier.Handler(DbFactory, new CreateSupplier.Validator(), Clock);
        UpdateSupplier = new UpdateSupplier.Handler(DbFactory, new UpdateSupplier.Validator());
        DeleteSupplier = new DeleteSupplier.Handler(DbFactory);
        ListSuppliers = new ListSuppliers.Handler(DbFactory);
        SetPrice = new SetSupplierPrice.Handler(DbFactory, new SetSupplierPrice.Validator(), Clock);
        DeletePrice = new DeleteSupplierPrice.Handler(DbFactory);
        ListPrices = new ListSupplierPrices.Handler(DbFactory);
        CreateLocation = new CreateStorageLocation.Handler(DbFactory, new CreateStorageLocation.Validator(), Clock);
        UpdateLocation = new UpdateStorageLocation.Handler(DbFactory, new UpdateStorageLocation.Validator());
        DeleteLocation = new DeleteStorageLocation.Handler(DbFactory);
        Receive = new ReceiveStock.Handler(DbFactory, new ReceiveStock.Validator(), Clock);
        Issue = new IssueStock.Handler(DbFactory, new IssueStock.Validator(), Clock);
        Transfer = new TransferStock.Handler(DbFactory, new TransferStock.Validator(), Clock);
        Overview = new ListStockOverview.Handler(DbFactory);
        ProductStock = new GetProductStock.Handler(DbFactory);
        Movements = new ListStockMovements.Handler(DbFactory);
        CreateCount = new CreateInventoryCount.Handler(DbFactory, new CreateInventoryCount.Validator(), Clock);
        GetCount = new GetInventoryCount.Handler(DbFactory);
        SaveCount = new SaveInventoryCountLines.Handler(DbFactory);
        PostCount = new PostInventoryCount.Handler(DbFactory, Clock);
        DeleteCount = new DeleteInventoryCount.Handler(DbFactory);
        ListCounts = new ListInventoryCounts.Handler(DbFactory);
    }

    public TimeProvider Clock { get; } = new FixedClock(new DateTimeOffset(2026, 11, 10, 12, 0, 0, TimeSpan.Zero));

    public IDbContextFactory<ProductsDbContext> DbFactory { get; }

    public CreateProduct.Handler Create { get; }

    public UpdateProduct.Handler Update { get; }

    public DeleteProduct.Handler Delete { get; }

    public ListProducts.Handler List { get; }

    public CreateProductCategory.Handler CreateCategory { get; }

    public DeleteProductCategory.Handler DeleteCategory { get; }

    public CreateSupplier.Handler CreateSupplier { get; }

    public UpdateSupplier.Handler UpdateSupplier { get; }

    public DeleteSupplier.Handler DeleteSupplier { get; }

    public ListSuppliers.Handler ListSuppliers { get; }

    public SetSupplierPrice.Handler SetPrice { get; }

    public DeleteSupplierPrice.Handler DeletePrice { get; }

    public ListSupplierPrices.Handler ListPrices { get; }

    public CreateStorageLocation.Handler CreateLocation { get; }

    public UpdateStorageLocation.Handler UpdateLocation { get; }

    public DeleteStorageLocation.Handler DeleteLocation { get; }

    public ReceiveStock.Handler Receive { get; }

    public IssueStock.Handler Issue { get; }

    public TransferStock.Handler Transfer { get; }

    public ListStockOverview.Handler Overview { get; }

    public GetProductStock.Handler ProductStock { get; }

    public ListStockMovements.Handler Movements { get; }

    public CreateInventoryCount.Handler CreateCount { get; }

    public GetInventoryCount.Handler GetCount { get; }

    public SaveInventoryCountLines.Handler SaveCount { get; }

    public PostInventoryCount.Handler PostCount { get; }

    public DeleteInventoryCount.Handler DeleteCount { get; }

    public ListInventoryCounts.Handler ListCounts { get; }

    public static ProductFields Hardware(string number = "HW-1", Guid? categoryId = null) =>
        new(number, "Cat6 cable 2m", ProductKind.Hardware, categoryId, "Blue", true, ProductUnit.Piece, 49.999m, 20m, 25m, "Acme", "C6-2", null, null, null);

    public void Dispose() => _connection.Dispose();

    private sealed class TestDbFactory(DbContextOptions<ProductsDbContext> options) : IDbContextFactory<ProductsDbContext>
    {
        public ProductsDbContext CreateDbContext() => new(options);
    }

    private sealed class FixedClock(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
