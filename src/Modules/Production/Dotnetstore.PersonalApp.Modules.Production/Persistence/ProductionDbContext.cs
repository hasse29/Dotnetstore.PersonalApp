using Dotnetstore.PersonalApp.Modules.Production.Domain;
using Microsoft.EntityFrameworkCore;

namespace Dotnetstore.PersonalApp.Modules.Production.Persistence;

internal sealed class ProductionDbContext(DbContextOptions<ProductionDbContext> options) : DbContext(options)
{
    public DbSet<UnitMeasure> UnitMeasures => Set<UnitMeasure>();

    public DbSet<ProductCategory> ProductCategories => Set<ProductCategory>();

    public DbSet<ProductSubcategory> ProductSubcategories => Set<ProductSubcategory>();

    public DbSet<ProductModel> ProductModels => Set<ProductModel>();

    public DbSet<ScrapReason> ScrapReasons => Set<ScrapReason>();

    public DbSet<Location> Locations => Set<Location>();

    public DbSet<Product> Products => Set<Product>();

    public DbSet<ProductCostHistory> ProductCostHistories => Set<ProductCostHistory>();

    public DbSet<ProductListPriceHistory> ProductListPriceHistories => Set<ProductListPriceHistory>();

    public DbSet<ProductInventory> ProductInventories => Set<ProductInventory>();

    public DbSet<ProductReview> ProductReviews => Set<ProductReview>();

    public DbSet<BillOfMaterial> BillOfMaterials => Set<BillOfMaterial>();

    public DbSet<WorkOrder> WorkOrders => Set<WorkOrder>();

    public DbSet<WorkOrderRouting> WorkOrderRoutings => Set<WorkOrderRouting>();

    public DbSet<TransactionHistory> TransactionHistories => Set<TransactionHistory>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ProductionDbContext).Assembly);
    }
}